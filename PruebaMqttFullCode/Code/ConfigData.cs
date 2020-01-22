using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MqttFramework.Code
{
    [Serializable]
    [DataContract(Namespace = "urn:rkiss.iot/tester/2016/12")]
    public class ConfigData : ConfigDataBase, IExtension<ServiceHostBase>
    {
        [DataMember]
        public string TopicAddress { get; set; }
        [DataMember]
        public string SubscriptionAddress { get; set; }
        [DataMember]
        public string TesterAddress { get; set; }
        [DataMember]
        public string HostName { get; set; }
        [DataMember]
        public bool RequiresSession { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public bool AutoReconnect { get; set; }

        public void Attach(ServiceHostBase owner) { }
        public void Detach(ServiceHostBase owner) { }

        public static XName XName { get { return XName.Get("ConfigData", "urn:rkiss.iot/tester/2016/12"); } }
    }
}
