using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using System.Threading;

namespace SendDirectMethodCall
{
    class Program
    {
        private const string HOST = "<iothubname.azure-devices.net>";
        private const int PORT = 8883;
        private const string DEVICE_ID = "<devicename>";
        private const string DEVICE_KEY = "<devicekey>";
        private const string methodName = "<methodNameToInvoke>";
        private readonly static string iotHubConnectionString = "<iothubconnectionstring>";


        private static ServiceClient s_serviceClient;
        private static async Task InvokeMethod()
        {
            var methodInvocation = new CloudToDeviceMethod(methodName) { ResponseTimeout = TimeSpan.FromSeconds(10), ConnectionTimeout = TimeSpan.FromSeconds(10) };
            methodInvocation.SetPayloadJson($"10");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await s_serviceClient.InvokeDeviceMethodAsync(DEVICE_ID, methodInvocation);

            Console.WriteLine("Response status: {0}, payload:", response.Status);
            Console.WriteLine(response.GetPayloadAsJson());
        }

        private static void Main(string[] args)
        {
            s_serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            while (true)
            {
                try
                {
                    InvokeMethod().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine("----------------------------------------------------------------------------------------------------------------------------------------\n");
                Thread.Sleep(10000);
            }


            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
