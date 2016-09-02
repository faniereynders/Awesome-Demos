using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebCamSample
{
    public static class LightController
    {
        public static async Task SetColor(string color, bool flash)
        {
            var client = await getClient();
            var command = new LightCommand();

            if (flash)
            {
                command.Alert = Alert.Multiple;
            }
            

            command.TurnOn().SetColor(color);
            await client.SendCommandAsync(command);
        }

        private static async Task<ILocalHueClient> getClient()
        {

            var locator = new HttpBridgeLocator();
            var bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            var ip = bridges.First();
            return new LocalHueClient(ip, App.Configuration.PhillipsHueAppKey);
        }
    }
}
