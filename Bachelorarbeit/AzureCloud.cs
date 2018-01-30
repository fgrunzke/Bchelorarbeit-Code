using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace Bachelorarbeit
{
    class AzureCloud
    {
        static RegistryManager registryManager;
        static string connectionString = def.connectionString;
        static string deviceKey = "";
        static DeviceClient deviceClient;
        static string deviceID = def.MY_RASPI;
        static string iotHubUri = def.iotHubUri;

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync().Wait();
            Console.ReadLine();
            Console.WriteLine("Simulated device\n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceID, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            deviceClient.ProductInfo = "HappyPath_Simulated-CSharp";
            SendDeviceToCloudMessagesAsync(10, 200, 1200, 30, 800, 50, 0.1, 1, 1.5);
            Console.ReadLine();
        }

        private static async Task AddDeviceAsync()
        {
            string deviceId = deviceID;
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceID));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            deviceKey = device.Authentication.SymmetricKey.PrimaryKey;
            Console.WriteLine("Generated device key: {0}", deviceKey);
        }

        private static async void SendDeviceToCloudMessagesAsync(int xStart, int yStart, int zStart, int xEnd, int yEnd, int zEnd, double xTime, double yTime, double zTime)
        {
            int messageId = 1;
            Random rand = new Random();

            var telemetryDataPoint = new
            {
                messageId = messageId++,
                deviceId = deviceID,
                xStart = xStart,
                yStart = yStart,
                zStart = zStart,
                xEnd = xEnd,
                yEnd = yEnd,
                zEnd = zEnd,
                xTime = xTime,
                yTime = yTime,
                zTime = zTime,
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
        }
    }
}
