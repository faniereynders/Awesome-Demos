using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot
{
    [Serializable]
    [LuisModel("8279aa14-ee0f-42b9-b17b-e826e42c34d9", "8369d13268d14267a0218c223c1e61f7")]
    public class AwesomeLuisDialog : LuisDialog<object>
    {

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {

            const string apiKey = "ea5e7640a337437b97a4d9da18a53bd3";

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
                    await Task.Run(() =>
                    {

                        AzureIoTHub.SendMessageAsync(JsonConvert.SerializeObject(hubMessage));
                    });
                }
                
               
                

                await context.PostAsync(replyMessage);


            }


          //  await context.PostAsync($"Sorry I did not understand");


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
        }
        
        [LuisIntent("select_winner")]
        public async Task SelectWinner(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Okay, let me pick a someone...");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("https://api.meetup.com/fixxup/events/231389959?fields=rsvp_sample&key=3b28723fa625e73c464d1235f4d3e&sign=true");
                var rsvps = await response.Content.ReadAsAsync<RSVPResponse>();

                var randomizer = new Random();
                var index = randomizer.Next(0, rsvps.rsvp_sample.Length - 1);

                var winner = rsvps.rsvp_sample[index].member;
                var text = $"The lucky winner is **{winner.name}** (party)";
                var activity = context.MakeMessage();

                activity.Text = text;
                activity.Attachments = new List<Attachment>() { new Attachment(contentType: "image/jpeg", contentUrl: winner.photo.photo_link) };
                // activity.Recipient = context.



                await context.PostAsync(activity);
            }

            context.Wait(MessageReceived);
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
