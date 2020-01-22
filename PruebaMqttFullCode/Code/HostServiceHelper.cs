using MqttFramework.Code;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MqttFramework.Code
{
    public class HostServiceHelper
    {
        public readonly int Port = 8883;
        private readonly string baseHostUrl = "net.pipe://localhost/AzureIoTHubTester_";
        private readonly string entity = "Device";
        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }

        public string IoTHubNamespace { get; set; }
        public HostServiceHelper(string deviceId, string deviceKey, string iotHubNamespace)
        {
            this.DeviceId = deviceId;
            this.DeviceKey = deviceKey;
            this.IoTHubNamespace = iotHubNamespace;
        }
        public void HostService()
        {
            ServiceHost host = null;
            try
            {
                DirectMethodHandler form = new DirectMethodHandler();
                string uriAddressString = null;

                string ports = ConfigurationManager.AppSettings["rangeOfPorts"];
                int[] rangeOfPorts = string.IsNullOrEmpty(ports) ?
                    new int[] { 20100, 20101, 20102, 20103, 20104, 20105, 20106, 20107, 20108, 20109 } :
                    ports.Split(',').Select(n => Convert.ToInt32(n)).ToArray();

                var usedPorts = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                for (int ii = 0; ii < rangeOfPorts.Length; ii++)
                {
                    if (usedPorts.FirstOrDefault(p => p.Port == rangeOfPorts[ii]) == null)
                    {
                        uriAddressString = string.Format(@"http://localhost:{0}/sb", rangeOfPorts[ii]);
                        break;
                    }
                };

                if (string.IsNullOrEmpty(uriAddressString))
                    throw new Exception("Not available port in the range 10100-10109");

                // interprocess communications
                var endpointAddress = new EndpointAddress(baseHostUrl + Process.GetCurrentProcess().Id);
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var se = new ServiceEndpoint(ContractDescription.GetContract(typeof(IGenericOneWayContract)), binding, endpointAddress);

                host = new ServiceHost(typeof(TesterService));
                host.AddServiceEndpoint(se);

                host.Extensions.Add(form);
                host.Open();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void ConfigureMqttListener()
        {
            string address = $"{this.IoTHubNamespace}.azure-devices.net";
            string username = $"{address}/{this.DeviceId}/?api-version=2018-06-30";
            string resourceUrl = $"{address}/devices/{this.DeviceId}";
            string password = SharedAccessSignatureBuilder.GetSASToken(username, this.DeviceKey, null, 24);
            string testerAddress = "net.pipe://localhost/AzureIoTHubTester_" + Process.GetCurrentProcess().Id;

            string id = Guid.NewGuid().ToString();
            ConfigData config = new ConfigData() { Name = this.DeviceId, Id = id, Username = username, Password = password, BrokerPort = this.Port, BrokerAddress = address, TesterAddress = testerAddress };

            string appDomainName = config.Name + "/" + config.Id;
            config.HostName = appDomainName;

            string contentTypeAndEncoding = $"$.ct=application%2Fjson&$.ce=utf-8";

            ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                try
                {
                    HostServices.Current.Add(appDomainName, config);
                    HostServices.Current.Open(appDomainName);

                }
                catch (Exception ex)
                {
                    HostServices.Current.Close(appDomainName);
                }
            });

        }
    }
}
