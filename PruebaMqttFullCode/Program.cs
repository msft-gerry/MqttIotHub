
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

namespace PruebaMqttFullCode
{
    class Program
    {
        static void Main(string[] args)
        {
            string deviceId = "<deviceId>";
            string deviceKey = "<deviceKey>";
            string iothubNamespace = "<iothubnamespace, without '.azure-devices.net'"; //example: iothubTest1;

            HostServiceHelper helper = new HostServiceHelper(deviceId, deviceKey, iothubNamespace);
            helper.HostService();
            helper.ConfigureMqttListener();


            Console.Read();
        }
    }
}

