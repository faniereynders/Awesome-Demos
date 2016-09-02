using Microsoft.Azure.Devices;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot.Infrastructure
{
    static class AzureIoTHub
    {
        public static async Task SendMessageAsync(string message)
        {
            var deviceConnectionString = ConfigurationManager.AppSettings["IoTHub:DeviceConnectionString"];


            var serviceClient = ServiceClient.CreateFromConnectionString(deviceConnectionString);


            var msg = new Message(Encoding.UTF8.GetBytes(message));

            await serviceClient.SendAsync("TestDevice", msg);
        }


    }
}
