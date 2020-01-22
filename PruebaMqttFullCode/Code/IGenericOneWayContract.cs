using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MqttFramework.Code
{
    [ServiceContract(Namespace = "urn:rkiss.iot/tester/2016/12")]
    public interface IGenericOneWayContract
    {
        [OperationContract(IsOneWay = true, Action = "*")]
        //[ReceiveContextEnabledAttribute(ManualControl=true)]
        void ProcessMessage(System.ServiceModel.Channels.Message msg);

        [OperationContract(IsOneWay = true, Action = "status")]
        void PostProcessMessage(string messageId, string status);
    }
}
