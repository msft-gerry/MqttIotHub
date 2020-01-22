using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MqttFramework.Code
{
    [Serializable]
    [DataContract(Namespace = "urn:rkiss.iot/tester/2016/12")]
    public class ConfigDataBase
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }
        [DataMember(Order = 1)]
        public string Type { get; set; }
        [DataMember(Order = 2)]
        public string DisplayName { get; set; }
        [DataMember(Order = 3)]
        public bool IsConnected { get; set; }
        [DataMember(Order = 4)]
        public string Id { get; set; }
        [DataMember(Order = 5)]
        public string BrokerAddress { get; set; }
        [DataMember(Order = 6)]
        public string Username { get; set; }
        [DataMember(Order = 7)]
        public string Password { get; set; }

        [DataMember(Order = 8)]
        public int BrokerPort { get; set; }
        [DataMember(Order = 9)]
        public string ContentType { get; set; }
    }
}
