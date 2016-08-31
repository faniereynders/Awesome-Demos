using Microsoft.Azure.Devices;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

static class AzureIoTHub
{
    //
    // Note: this connection string is specific to the device "TestDevice". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    
    //
    // To monitor messages sent to device "TestDevice" use iothub-explorer as follows:
    //    iothub-explorer HostName=fanie-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=kWNJzx/q5s5CsrUsyUfEH5tttRUf+LwBlKrNJ6/DA2U= monitor-events "TestDevice"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendMessageAsync(string message)
    {
        var deviceConnectionString = ConfigurationManager.AppSettings["IoTHub:DeviceConnectionString"];
        

        var serviceClient = ServiceClient.CreateFromConnectionString(deviceConnectionString);
        
        
        var msg = new Message(Encoding.UTF8.GetBytes(message));

        await serviceClient.SendAsync("TestDevice", msg);
    }

   
}
