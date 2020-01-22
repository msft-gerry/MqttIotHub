using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MqttFramework.Code
{
    public class MqttMsgEventArgs
    {
        public MqttMsgEventArgs()
        {
        }
        public MqttMsgEventArgs(string topic, byte[] message, bool dupFlag, byte qosLevel, bool retain)
        {
            Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Topic = topic;
            Message = message;
            DupFlag = dupFlag;
            QosLevel = qosLevel;
            Retain = retain;
        }
        public MqttMsgEventArgs(MqttMsgPublishEventArgs args) : this(args.Topic, args.Message, args.DupFlag, args.QosLevel, args.Retain) { }

        public string Timestamp { get; set; }
        public bool DupFlag { get; set; }
        public byte[] Message { get; set; }
        public byte QosLevel { get; set; }
        public bool Retain { get; set; }
        public string Topic { get; set; }

    }
}
