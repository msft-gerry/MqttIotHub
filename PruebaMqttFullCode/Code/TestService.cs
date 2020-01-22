using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;


namespace MqttFramework.Code
{
    public class TesterService : IGenericOneWayContract
    {
        public void ProcessMessage(Message message)
        {
            Trace.WriteLine("TesterService: === Message has been received ===");
            Trace.WriteLine(message.ToString());

            DirectMethodHandler form = OperationContext.Current.Host.Extensions.Find<DirectMethodHandler>();
            string action = OperationContext.Current.IncomingMessageHeaders.Action;

            int indexConfig = message.Headers.FindHeader(ConfigData.XName.LocalName, ConfigData.XName.NamespaceName);

            try
            {
                var config = message.Headers.GetHeader<ConfigData>(indexConfig);
                message.Headers.RemoveAt(indexConfig);
                var payload = JsonConvert.DeserializeObject<MqttMsgEventArgs>(message.GetBody<string>());
                form.ProcessMessage(payload, config);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

       

        public void PostProcessMessage(string messageId, string status)
        {
            //Form1 form = OperationContext.Current.Host.Extensions.Find<Form1>();
            //string action = OperationContext.Current.IncomingMessageHeaders.Action;
            //int indexConfig = OperationContext.Current.IncomingMessageHeaders.FindHeader(ConfigData.XName.LocalName, ConfigData.XName.NamespaceName);

            //try
            //{
            //    var config = OperationContext.Current.IncomingMessageHeaders.GetHeader<ConfigData>(indexConfig);
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }
    }
}
