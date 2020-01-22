using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace MqttFramework.Code
{
    public class DirectMethodHandler : IExtension<ServiceHostBase>
    {
        public void ProcessMessage(MqttMsgEventArgs payload, ConfigData config)
        {
            if (payload.Topic.StartsWith("$iothub/methods/POST/"))
            {

                var methodName = payload.Topic.Replace("$iothub/methods/POST/", "").Split('?').FirstOrDefault()?.Trim('/');
                var topicparts = payload.Topic.Split(new[] { '?' }, 2);
                var dic = topicparts.Last().Split('&').ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

                string topicResponse = $"$iothub/methods/res/200/?$rid={dic["$rid"]}";
                RespondToDirectMethod(config, topicResponse);
            }
        }

        private void RespondToDirectMethod(ConfigData config, string topicResponse)
        {
            string name = config.BrokerAddress + "/" + config.Name;
            var client = HostServices.Current.GetClient(name);

            var code = client.Publish(topicResponse, "{'accepted':1}", 1, false);
        }



        #region IExtension<ServiceHostBase> Members
        public void Attach(ServiceHostBase owner) { }
        public void Detach(ServiceHostBase owner) { }
        #endregion
    }
}