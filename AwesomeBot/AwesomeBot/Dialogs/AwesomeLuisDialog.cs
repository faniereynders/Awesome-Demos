using AwesomeBot.Infrastructure;
using AwesomeBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot.Dialogs
{
    [Serializable]
    public class AwesomeLuisDialog : LuisDialog<object>
    {
        private static ILuisService service
        {
            get
            {
                return new LuisService(
                    new LuisModelAttribute(
                        ConfigurationManager.AppSettings["LUIS:ModelId"], ConfigurationManager.AppSettings["LUIS:SubscriptionKey"]));
            }
        }
        public AwesomeLuisDialog():base(service) { }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {

            var apiKey = ConfigurationManager.AppSettings["MsCognitive:TextAnalytics"];

            string queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            using (var client = new HttpClient())
            {
                
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                var sentimentInput = new BatchInput();

                sentimentInput.documents = new List<DocumentInput>();
                sentimentInput.documents.Add(new DocumentInput()
                {
                    id = 1,
                    text = result.Query
                });

                var sentimentJsonInput = JsonConvert.SerializeObject(sentimentInput);
                byte[] byteData = Encoding.UTF8.GetBytes(sentimentJsonInput);
                var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var sentimentPost = await client.PostAsync(queryUri, content);
                var sentimentRawResponse = await sentimentPost.Content.ReadAsStringAsync();
                var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchResult>(sentimentRawResponse);
                double sentimentScore = sentimentJsonResponse.documents[0].score;

                var replyMessage = context.MakeMessage();
                
                if (sentimentScore < 0.7 && sentimentScore > 0.3)
                {
                    replyMessage.Text = $"I see...";
                    
                }
                else
                {

                    var hubMessage = new HubMessage();
                    if (sentimentScore > 0.7)
                    {

                        replyMessage.Text = $"That's great to hear!";
                        hubMessage.Command = "flash-color";
                        hubMessage.Payload = "#00cc00";
                    }
                    else if (sentimentScore < 0.3)
                    {
                        replyMessage.Text = $"I'm sorry to hear that...";
                        hubMessage.Payload = "#ff0000";
                        hubMessage.Command = "flash-color";
                    }
                    await Task.Run(async () =>
                    {

                        await AzureIoTHub.SendMessageAsync(JsonConvert.SerializeObject(hubMessage));
                    });
                }
                
                await context.PostAsync(replyMessage);
                
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("greet")]
        public async Task Greet(IDialogContext context, LuisResult result)
        {
            var message = context.MakeMessage();
            message.Text = $"Well hello there {message.Recipient.Name}";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("start_track_emotion")]
        public async Task StartTrackingEmotion(IDialogContext context, LuisResult result)
        {
            var trackingStarted = false;
            context.UserData.TryGetValue<bool>("emotion-tracking-started", out trackingStarted);
            if (!trackingStarted)
            {
                PromptDialog.Confirm(context, confirmEmotionTracking,
                    "I will now use the webcam for emotion analysis and the results wil go to Skype as emoij's. Is that okay?",
                    "Didn't get that! Please try again?",
                    promptStyle: PromptStyle.None);
                
            }
            else
            {
                await context.PostAsync("I'm already analyzing emotions. Tell me when to stop...");
                context.Wait(MessageReceived);
            }
            
        }

        [LuisIntent("stop_track_emotion")]
        public async Task StopTrackingEmotion(IDialogContext context, LuisResult result)
        {
            context.UserData.SetValue("emotion-tracking-started", false);
            var hubMessage = new
            {
                command = "stop-track"
            };
            await AzureIoTHub.SendMessageAsync(JsonConvert.SerializeObject(hubMessage));
            await context.PostAsync("I've stopped tracking emotions.");
            context.Wait(MessageReceived);
        }
        
        [LuisIntent("select_winner")]
        public async Task SelectWinner(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Okay, let me pick a someone...");
            var contents = File.ReadAllText($@"{AppContext.BaseDirectory}\people.json");
            var people = JsonConvert.DeserializeObject<string[]>(contents);
                var randomizer = new Random();
                var index = randomizer.Next(0, people.Length - 1);
                var winnerName = people[index];
                var winnerImageUrl = await getImageUrlFromBing(winnerName);
            
                var text = $"The lucky winner is **{winnerName}** (party)";
                var activity = context.MakeMessage();

                activity.Text = text;
                activity.Attachments = new List<Attachment>() { new Attachment(contentType: "image/jpg", contentUrl: winnerImageUrl) };
                // activity.Recipient = context.



                await context.PostAsync(activity);


            context.Wait(MessageReceived);
        }

        private async Task<string> getImageUrlFromBing(string name)
        {
            using (var client = new HttpClient())
            {
                var apiKey = ConfigurationManager.AppSettings["MsCognitive:BingSearch"];
                var id = ConfigurationManager.AppSettings["BotId"];

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var uri = $"https://api.cognitive.microsoft.com/bing/v5.0/images/search?q={name}&count=1";
                var response = await client.GetAsync(uri);
                var content = await response.Content.ReadAsAsync<dynamic>();

                return content.value[0].thumbnailUrl;
               
            }
        }

        private async Task confirmEmotionTracking(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                context.UserData.SetValue("emotion-tracking-started", true);
                var hubMessage = new
                {
                    command = "start-track"
                };
                await AzureIoTHub.SendMessageAsync(JsonConvert.SerializeObject(hubMessage));
                await context.PostAsync("Busy tracking emotion changes...");
            }
            else
            {
                await context.PostAsync("Okay");
            }
            context.Wait(MessageReceived);
        }

    }
}
