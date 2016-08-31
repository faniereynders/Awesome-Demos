using System.Threading.Tasks;
using System;
using Newtonsoft.Json;

namespace WebCamSample
{
    public class Configuration
    {
        
        public static Configuration LoadFrom(string fileName)
        {
            var packageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var contents = Task.Run(async () => {
                var file = await packageFolder.GetFileAsync(fileName);
                return await Windows.Storage.FileIO.ReadTextAsync(file);
            });

            return JsonConvert.DeserializeObject<Configuration>(contents.Result);
        }
        public string IoTHubConnectionString { get; set; }
        public string PhillipsHueAppKey { get; set; }
        public string MsCognitiveEmotion { get; set; }
    }
}
