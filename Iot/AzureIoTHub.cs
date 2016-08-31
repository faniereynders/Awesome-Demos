using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Windows.UI.Xaml;
using WebCamSample;

public class MyClass
{
    public event EventHandler<string> OnChanged;
    public void Invoke()
    {
        OnChanged(this, "A");
    }
}

public static class Global
{
    public static MyClass handler = new MyClass();
   // public MyClass MyProperty { get; set; }
}

public class HubMessage
{
    public string Command { get; set; }
    public dynamic Payload { get; set; }
}

static class AzureIoTHub
{

    
    //
    // Note: this connection string is specific to the device "TestDevice". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    //const string deviceConnectionString = "HostName=fanie-hub.azure-devices.net;DeviceId=TestDevice;SharedAccessKey=2u1S3oXfAoYuTm0YJc6RqVzQyYKBIbmKkcSOKbZYgdM=";

    //
    // To monitor messages sent to device "TestDevice" use iothub-explorer as follows:
    //    iothub-explorer HostName=fanie-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=kWNJzx/q5s5CsrUsyUfEH5tttRUf+LwBlKrNJ6/DA2U= monitor-events "TestDevice"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendDeviceToCloudMessageAsync()
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(App.Configuration.IoTHubConnectionString, TransportType.Http1);

        var str = "Hello, Cloud!";
        var message = new Message(Encoding.ASCII.GetBytes(str));

        await deviceClient.SendEventAsync(message);
    }
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
