using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MqttFramework.Code
{
    public sealed class MqttClientActivator : MarshalByRefObject, IDisposable
    {
        DateTime _created = DateTime.Now;
        string _name = string.Empty;
        MqttClient _client = null;
        ConfigData _configData = null;

        #region Dispose
        void IDisposable.Dispose()
        {
            Disconnect();
        }
        public override object InitializeLifetimeService()
        {
            // infinite lifetime
            return null;
        }
        #endregion

        #region Create
        public static MqttClientActivator Create(AppDomain appDomain, ConfigData config)
        {
            string _assemblyName = Assembly.GetAssembly(typeof(MqttClientActivator)).FullName;
            MqttClientActivator activator = appDomain.CreateInstanceAndUnwrap(_assemblyName, typeof(MqttClientActivator).ToString()) as MqttClientActivator;
            activator.SetClient(config);
            return activator;
        }

        private void SetClient(ConfigData config)
        {
            try
            {
                if (_client == null)
                {
                    _client = new MqttClient(config.BrokerAddress.Trim(), config.BrokerPort, true, null, null, MqttSslProtocols.TLSv1_2);
                    _client.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;

                    // event when connection has been dropped
                    _client.ConnectionClosed += Client_ConnectionClosed;

                    // handler for received messages on the subscribed topics
                    _client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                    // handler for publisher
                    _client.MqttMsgPublished += Client_MqttMsgPublished;

                    // handler for subscriber 
                    _client.MqttMsgSubscribed += Client_MqttMsgSubscribed;

                    // handler for unsubscriber
                    _client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;

                    _name = config.BrokerAddress + "/" + config.Name;

                    this._configData = config;
                    this.AddToStorage(this);

                    LogMessage($"[{this.Name}] Client has been created in the appDomain {AppDomain.CurrentDomain.FriendlyName}");
                }
                else
                {
                    throw new InvalidOperationException("The MqttClient has been already setup");
                }
            }
            finally
            {
                CallContext.FreeNamedDataSlot("_config");
            }
        }

        #region events
        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            LogMessage($"[{this.Name}] Connection closed", "Warning");
            if (_configData.AutoReconnect == false)
            {
                var payload = new MqttMsgEventArgs("$iothub/clientproxy/", Encoding.UTF8.GetBytes("Disconnected"), false, 0, false);
                this.ForwardMessageAsync(payload).Wait();
            }
            else
            {
                Thread.Sleep(500);
                this.Connect(null, true);
            }
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            LogMessage($"[{this.Name}] Subscriber received at {e.Topic}", "HighlightInfo");
            this.ForwardMessageAsync(new MqttMsgEventArgs(e)).Wait();
        }

        private void Client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            LogMessage($"[{this.Name}] Response from publish {e.MessageId.ToString()}");
        }

        private void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            LogMessage($"[{this.Name}] Response from subscribe {e.MessageId.ToString()}");
        }
        private void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            LogMessage($"[{this.Name}] Response from unsibscribe {e.MessageId.ToString()}");
        }
        #endregion

        private void AddToStorage(MqttClientActivator activator)
        {
            List<MqttClientActivator> activators = this.GetStorage();
            if (activators.Exists(delegate (MqttClientActivator host) { return host.Name == activator.Name; }))
            {
                LogMessage($"[{this.Name}] Internal Error during add appDomain {AppDomain.CurrentDomain.FriendlyName} to storage, activator alredy exist it", "Error");
                throw new InvalidOperationException(string.Format("The client '{0}' is already hosted in the appDomain '{1}'", activator.Name, AppDomain.CurrentDomain.FriendlyName));
            }
            activators.Add(this);
        }
        private void RemoveFromStorage(MqttClientActivator activator)
        {
            List<MqttClientActivator> activators = this.GetStorage();
            if (activators.Exists(delegate (MqttClientActivator host) { return host.Name == activator.Name; }))
            {
                activators.Remove(activator);
            }
        }
        private List<MqttClientActivator> GetStorage()
        {
            string key = typeof(MqttClientActivator).FullName;
            List<MqttClientActivator> activators = AppDomain.CurrentDomain.GetData(key) as List<MqttClientActivator>;
            if (activators == null)
            {
                lock (AppDomain.CurrentDomain.FriendlyName)
                {
                    activators = AppDomain.CurrentDomain.GetData(key) as List<MqttClientActivator>;
                    if (activators == null)
                    {
                        activators = new List<MqttClientActivator>();
                        AppDomain.CurrentDomain.SetData(key, activators);
                    }
                }
            }
            return activators;
        }
        #endregion

        #region Remoting
        public bool Connect(string password = null, bool reConnect = false)
        {
            if (_client != null)
            {
                try
                {
                    if (_client.IsConnected == false)
                    {
                        // update a SAS for reconnection
                        if (string.IsNullOrEmpty(password) == false)
                            this._configData.Password = password;

                        // device & module
                        var parts = _configData.Name.Split(new char[] { '/' }, 2);
                        string dm = parts.Count() == 2 ? $"{parts.First()}/modules/{parts.Last()}" : parts.First();
                        string willTopic = $"devices/{dm}/messages/events/willmessage=disconnected";
                        string willMessage = "{\"msg\":\"WillMessage from the IoT Hub tester\"}";

                        byte connCode = _client.Connect(this._configData.Name, this._configData.Username, this._configData.Password, false, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true, willTopic, willMessage, false, 300); // true
                        if (connCode != 0 || _client == null || _client.IsConnected == false)
                            throw new Exception($"Connect failed, code = {connCode}");

                        // update config
                        _configData.AutoReconnect = reConnect;
                        LogMessage(reConnect ? $"[{this.Name}] Re-Connected" : $"[{this.Name}] Connected", "HighlightInfo");

                        //Thread.Sleep(1000);

                        // C2D messaging
                        var _topics = new string[] { $"devices/{dm}/messages/devicebound/#" };
                        var subCode = _client.Subscribe(_topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                        LogMessage($"[{this.Name}] Subscribing_{subCode}: {string.Join("  ", _topics)}");

                        // device twin
                        _topics = new string[] { "$iothub/methods/POST/#", "$iothub/twin/res/#", "$iothub/twin/PATCH/properties/desired/#" };
                        subCode = _client.Subscribe(_topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                        LogMessage($"[{this.Name}] Subscribing_{subCode}: {string.Join("  ", _topics)}");

                        // device streams
                        _topics = new string[] { $"$iothub/streams/POST/#" };
                        subCode = _client.Subscribe(_topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                        LogMessage($"[{this.Name}] Subscribing_{subCode}: {string.Join("  ", _topics)}");

                    }
                    return _client.IsConnected;
                }
                catch (Exception ex)
                {
                    if (_client != null)
                        _configData.AutoReconnect = false;
                    RemoveFromStorage(this);
                    LogMessage($"[{this.Name}] Connecting device failed: {ex.Message}", "Error");
                    //throw ex;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public void Disconnect(bool bLog = false)
        {
            if (_client != null)
                _configData.AutoReconnect = false;

            if (_client != null && _client.IsConnected)
            {
                try
                {
                    //if(_topics != null)
                    //    _client.Unsubscribe(_topics);  
                    _client.Disconnect();
                    if (bLog)
                        LogMessage($"[{this.Name}] Client has been disconnected and the appDomain {AppDomain.CurrentDomain.FriendlyName} is going to be unloaded");
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // one more change to disconnect
                        if (_client.IsConnected)
                        {
                            _client.Disconnect();
                            if (bLog)
                                LogMessage($"[{this.Name}] After one more time, the Client has been disconnected and the appDomain {AppDomain.CurrentDomain.FriendlyName} is going to be unloaded");
                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex2)
                    {
                        throw new Exception($"Disconnect device failed, error = {ex2.Message}");
                    }
                    finally
                    {
                        if (bLog)
                            LogMessage($"[{this.Name}] Disconnecting device failed: {ex.Message}", "Error");
                    }
                }
            }
            else
            {
                if (bLog)
                    LogMessage($"[{this.Name}] Client has been already disconnected and the appDomain {AppDomain.CurrentDomain.FriendlyName} is going to be unloaded");
            }
        }
        public void Abort()
        {
            if (_client != null)
            {
                try
                {
                    //_host.Abort();
                    Trace.WriteLine(string.Format("ServiceHostActivator '{0}' aborted", this.Name));
                }
                catch (Exception ex)
                {
                    LogMessage($"[{this.Name}] Aborting device failed: {ex.Message}", "Error");
                    throw ex;
                }
            }
        }
        public MqttClient Client { get { return _client; } } // valid only for default domain! 
        public AppDomain AppDomainHost { get { return AppDomain.CurrentDomain; } }
        public string Name { get { return _name; } }
        public DateTime Created { get { return _created; } }
        public ushort Publish(string topic, string payload, byte qos, bool retain)
        {
            if (_client.IsConnected)
                return _client.Publish(topic, Encoding.UTF8.GetBytes(payload), qos, retain);
            else
                return 0;
        }
        #endregion

        #region WCF
        private async Task ForwardMessageAsync(MqttMsgEventArgs e)
        {
            await Task.Run(() =>
            {
                ChannelFactory<IGenericOneWayContract> factory = null;
                try
                {
                    var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                    var se = new ServiceEndpoint(ContractDescription.GetContract(typeof(IGenericOneWayContract)), binding, new EndpointAddress(this._configData.TesterAddress));
                    factory = new ChannelFactory<IGenericOneWayContract>(se);
                    var channel = factory.CreateChannel();

                    using (var scope = new OperationContextScope((IContextChannel)channel))
                    {
                        var message = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, "*", JsonConvert.SerializeObject(e));
                        message.Headers.Add(MessageHeader.CreateHeader(ConfigData.XName.LocalName, ConfigData.XName.NamespaceName, this._configData));
                        channel.ProcessMessage(message);

                        Trace.WriteLine("VirtualService: --- Message has been sent to tester ---");
                    }
                    factory.Close();
                }
                catch (CommunicationException ex)
                {
                    if (factory != null)
                    {
                        if (factory.State == CommunicationState.Faulted)
                            factory.Abort();
                        else if (factory.State != CommunicationState.Closed)
                            factory.Close();
                        factory = null;
                    }
                    Trace.WriteLine(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    if (factory != null)
                    {
                        if (factory.State == CommunicationState.Faulted)
                            factory.Abort();
                        else if (factory.State != CommunicationState.Closed)
                            factory.Close();
                        factory = null;
                    }
                    Trace.WriteLine(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                }
            });
        }

        public void LogMessage(string message, string severity = "Info")
        {
            var payload = new MqttMsgEventArgs($"$iothub/logmessage/{severity}", Encoding.UTF8.GetBytes($"{DateTime.Now.ToLocalTime().ToString("yyyy-MM-ddTHH:MM:ss.fff")}: {message}"), false, 0, false);
            this.ForwardMessageAsync(payload).Wait();
        }
        #endregion
    }
}
