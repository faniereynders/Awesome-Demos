using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Windows.UI.Xaml;
using WebCamSample;

public class HubMessage
{
    public string Command { get; set; }
    public dynamic Payload { get; set; }
}

static class AzureIoTHub
{
    public static async Task SendToHubAsync(object message)
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(App.Configuration.IoTHubConnectionString, TransportType.Http1);

        var str = JsonConvert.SerializeObject(message);

     
        var msg = new Message(Encoding.UTF8.GetBytes(str));

        await deviceClient.SendEventAsync(msg);
    }
    public static async Task<HubMessage> ReceiveCloudToDeviceMessageAsync()
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(App.Configuration.IoTHubConnectionString, TransportType.Http1);

        while (true)
        {
            var receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                var messageData = JsonConvert.DeserializeObject<HubMessage>(Encoding.UTF8.GetString(receivedMessage.GetBytes()));
                
                await deviceClient.CompleteAsync(receivedMessage);

                
                return messageData;
            }

            //  Note: In this sample, the polling interval is set to 
            //  10 seconds to enable you to see messages as they are sent.
            //  To enable an IoT solution to scale, you should extend this 
            //  interval. For example, to scale to 1 million devices, set 
            //  the polling interval to 25 minutes.
            //  For further information, see
            //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }
}
