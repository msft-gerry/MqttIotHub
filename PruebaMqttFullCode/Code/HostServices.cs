using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MqttFramework.Code
{
    public sealed class HostServices : IDisposable
    {
        #region Private Members
        ReaderWriterLock _rwl = new ReaderWriterLock();
        string _Name = string.Empty;
        List<AppDomain> _appDomains = new List<AppDomain>();
        static string _key = typeof(MqttClientActivator).FullName;
        bool _selfHosted = false;
        #endregion

        #region Constructors
        public HostServices()
            : this(false)
        {
        }
        public HostServices(bool selfHosted)
        {
            _selfHosted = selfHosted;
            Trace.WriteLine(string.Format("The HostServices '{0}' runtime has been created, selfHosted={1}", _Name, selfHosted));
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                _appDomains.Clear();
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
        }
        #endregion

        #region Name
        public string Name
        {
            get { return _Name; }
        }
        #endregion

        #region Add
        public void Add(string appDomainName, ConfigData config)
        {
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                appDomainName = ValidateAppDomainName(appDomainName);
                AppDomain appDomain = this.CreateDomainHost(appDomainName);
                MqttClientActivator.Create(appDomain, config);
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
        }

        private AppDomain CreateDomainHost(string appDomainName)
        {
            AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });
            if (appDomain == null)
            {
                appDomain = AppDomain.CurrentDomain.FriendlyName == appDomainName ? AppDomain.CurrentDomain : AppDomain.CreateDomain(appDomainName);
                _appDomains.Add(appDomain);
                Trace.WriteLine(string.Format("The AppDomain '{0}' has been created", appDomainName));

                appDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
                {
                    Trace.WriteLine(string.Format("[{0}] UnhandledException = {1}", (sender as AppDomain).FriendlyName, e.ExceptionObject));
                };
                appDomain.DomainUnload += delegate (object sender, EventArgs e)
                {
                    Trace.WriteLine(string.Format("[{0}] DomainUnload", (sender as AppDomain).FriendlyName));
                };
                appDomain.ProcessExit += delegate (object sender, EventArgs e)
                {
                    Trace.WriteLine(string.Format("[{0}] ProcessExit", (sender as AppDomain).FriendlyName));
                };
            }
            return appDomain;
        }
        private string ValidateAppDomainName(string appDomainName)
        {
            if (string.IsNullOrEmpty(appDomainName) || appDomainName == "*" || appDomainName.ToLower() == "default")
            {
                appDomainName = AppDomain.CurrentDomain.FriendlyName;
            }
            return appDomainName;
        }
        #endregion

        #region IsDomainExists
        public bool IsDomainExists(string appDomainName)
        {
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                appDomainName = ValidateAppDomainName(appDomainName);
                AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });
                return appDomain != null;
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }

        }
        #endregion

        #region GetAppDomainHost
        public AppDomain this[string appDomainName]
        {
            get
            {
                try
                {
                    _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                    appDomainName = ValidateAppDomainName(appDomainName);
                    AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });
                    if (appDomain != null)
                    {
                        return appDomain;
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Requested appDomain '{0}' doesn't exist in the catalog", appDomainName));
                    }
                }
                finally
                {
                    _rwl.ReleaseWriterLock();
                }
            }
        }
        #endregion

        #region GetHostedServices
        public List<ServiceHostActivatorStatus> GetHostedServices
        {
            get
            {
                List<ServiceHostActivatorStatus> lists = new List<ServiceHostActivatorStatus>();
                try
                {
                    _rwl.AcquireReaderLock(TimeSpan.FromSeconds(60));
                    foreach (AppDomain appDomain in _appDomains)
                    {
                        List<MqttClientActivator> activators = appDomain.GetData(_key) as List<MqttClientActivator>;
                        if (activators != null)
                        {
                            foreach (MqttClientActivator activator in activators)
                            {
                                lists.Add(new ServiceHostActivatorStatus
                                {
                                    Created = activator.Created,
                                    AppDomainHostName = activator.AppDomainHost.FriendlyName,
                                    //State=activator.State,
                                    Name = activator.Name,
                                    //SubscriptionAddress = activator.

                                });
                            }
                        }
                    }
                }
                finally
                {
                    _rwl.ReleaseReaderLock();
                }
                return lists;
            }
        }
        #endregion

        #region GetHostedService
        public MqttClientActivator GetClient(string name, bool flagRemove = false)
        {
            MqttClientActivator client = null;
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Not valid service name");

            try
            {
                _rwl.AcquireReaderLock(TimeSpan.FromSeconds(60));
                foreach (AppDomain appDomain in _appDomains)
                {
                    List<MqttClientActivator> activators = appDomain.GetData(_key) as List<MqttClientActivator>;
                    if (activators != null)
                    {
                        MqttClientActivator activator = activators.Find(delegate (MqttClientActivator a) { return a.Name == name; });
                        if (activator != null)
                        {
                            client = activator;
                            if (flagRemove)
                            {
                                activators.Remove(activator);
                            }
                        }
                    }
                }
            }
            finally
            {
                _rwl.ReleaseReaderLock();
            }
            return client;

        }
        #endregion

        #region Open
        public int Open()
        {
            int count = 0;
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                foreach (AppDomain ad in _appDomains)
                {
                    List<MqttClientActivator> activators = ad.GetData(_key) as List<MqttClientActivator>;
                    if (activators != null)
                    {
                        activators.ForEach(delegate (MqttClientActivator activator)
                        {
                            activator.Connect();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                count = _appDomains.Count;
                _rwl.ReleaseWriterLock();
            }
            return count;
        }
        public void Open(string appDomainName, string password = null, bool autoReconnect = false)
        {
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                appDomainName = ValidateAppDomainName(appDomainName);
                AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });
                if (appDomain != null)
                {
                    if (appDomain.IsDefaultAppDomain() == false)
                    {
                        List<MqttClientActivator> activators = appDomain.GetData(_key) as List<MqttClientActivator>;
                        if (activators != null)
                        {
                            activators.ForEach(delegate (MqttClientActivator activator)
                            {
                                Trace.WriteLine(string.Format("Open service '{0}'", activator.Name));
                                activator.Connect(password, autoReconnect);
                            });
                            Trace.WriteLine(string.Format("Open services in the AppDomain '{0}'", appDomainName));
                        }
                        else
                        {
                            Trace.WriteLine(string.Format("None services for opening in the AppDomain '{0}'", appDomainName));
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Open '{0}' appDomain host failed - doesn't exist", appDomainName));
                }
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
        }
        #endregion

        #region Close
        public void Close()
        {
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));
                _appDomains.Reverse();
                foreach (AppDomain ad in _appDomains)
                {
                    List<MqttClientActivator> activators = ad.GetData(_key) as List<MqttClientActivator>;
                    if (activators != null)
                    {
                        activators.Reverse();
                        activators.ForEach(delegate (MqttClientActivator activator)
                        {
                            activator.Disconnect();
                        });
                        activators.Clear();
                    }
                }

                int count = _appDomains.RemoveAll(delegate (AppDomain ad)
                {
                    if (!ad.IsDefaultAppDomain())
                    {
                        AppDomain.Unload(ad);
                    }
                    return true;
                });
                _appDomains.Clear();
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
        }
        public void Close(string appDomainName)
        {
            this.Close(appDomainName, true);
        }
        public bool Close(string appDomainName, bool bThrow = false)
        {
            bool bHasBeenClosed = false;
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));

                appDomainName = ValidateAppDomainName(appDomainName);
                AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });
                if (appDomain != null)
                {
                    if (appDomain.IsDefaultAppDomain() == false)
                    {

                        List<MqttClientActivator> activators = appDomain.GetData(_key) as List<MqttClientActivator>;
                        if (activators != null)
                        {
                            activators.Reverse();
                            activators.ForEach(delegate (MqttClientActivator activator)
                            {
                                activator.Disconnect(true);
                            });
                            activators.Clear();
                        }

                        // clean-up                        
                        _appDomains.Remove(appDomain);
                        AppDomain.Unload(appDomain);
                        bHasBeenClosed = true;
                    }
                    else if (bThrow)
                    {
                        throw new InvalidOperationException("The Close operation can't be processed on the default appDomain");
                    }
                }
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
            return bHasBeenClosed;
        }
        #endregion

        #region Abort
        public bool Abort(string appDomainName, bool bThrow)
        {
            bool bHasBeenAborted = false;
            try
            {
                _rwl.AcquireWriterLock(TimeSpan.FromSeconds(60));

                appDomainName = ValidateAppDomainName(appDomainName);
                AppDomain appDomain = _appDomains.Find(delegate (AppDomain ad) { return ad.FriendlyName == appDomainName; });

                if (appDomain != null)
                {
                    if (appDomain.IsDefaultAppDomain() == false)
                    {
                        List<MqttClientActivator> activators = appDomain.GetData(_key) as List<MqttClientActivator>;
                        activators.Reverse();
                        if (activators != null)
                        {
                            activators.ForEach(delegate (MqttClientActivator activator)
                            {
                                activator.Abort();
                            });
                        }

                        // clean-up
                        activators.Clear();
                        _appDomains.Remove(appDomain);
                        AppDomain.Unload(appDomain);
                        bHasBeenAborted = true;
                    }
                    else if (bThrow)
                    {
                        throw new InvalidOperationException("The Abort operation can't be processed on the default appDomain");
                    }
                }
            }
            finally
            {
                _rwl.ReleaseWriterLock();
            }
            return bHasBeenAborted;
        }
        #endregion

        #region Current
        public static HostServices Current
        {
            get
            {
                string key = typeof(HostServices).FullName;
                HostServices hostservices = AppDomain.CurrentDomain.GetData(key) as HostServices;
                if (hostservices == null)
                {
                    lock (AppDomain.CurrentDomain.FriendlyName)
                    {
                        hostservices = AppDomain.CurrentDomain.GetData(key) as HostServices;
                        if (hostservices == null)
                        {
                            hostservices = new HostServices(true);
                            AppDomain.CurrentDomain.SetData(key, hostservices);
                        }
                    }
                }
                return hostservices;
            }
        }
        #endregion
    }
}
