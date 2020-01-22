using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MqttFramework.Code
{
    [DataContract(Namespace = "urn:rkiss.iot/tester/2016/12")]
    public class ServiceHostActivatorStatus : ConfigData
    {
        [DataMember]
        public CommunicationState State { get; set; }
        [DataMember]
        public DateTime Created { get; set; }
        [DataMember]
        public string AppDomainHostName { get; set; }

    }

}
