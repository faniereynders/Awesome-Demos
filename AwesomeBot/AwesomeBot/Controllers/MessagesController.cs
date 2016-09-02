using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;

using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;
using AwesomeBot.Dialogs;
using AwesomeBot.Infrastructure;

namespace AwesomeBot
{
    // [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<IHttpActionResult> Post([FromBody]Activity activity)
        {
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            if (activity.Type == ActivityTypes.Message)
            {
                if (activity.Attachments != null && activity.Attachments.Any(_ => _.ContentType.StartsWith("image")))
                {
                    await imageReceived(connector, activity);
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new AwesomeLuisDialog());
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }

            return Ok();
        }

        private async Task imageReceived(IConnectorClient connector, Activity activity)
        {
            var reply = activity.CreateReply($"**Yay!** It seems that you have sent me a picture. Let me take a look... (gift)");
            await connector.Conversations.ReplyToActivityAsync(reply);

            var token = string.Empty;
            
            var imageResult = await ImageAnalyzer.DescribeImage(activity.Attachments[0].ContentUrl, activity.ServiceUrl);

            var colorHex = imageResult.Color.AccentColor;

            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($@"I see {imageResult.Description.Captions[0].Text}."));

            await Task.Run(async () =>
            {
                var hubMessage = new
                {
                    command = "set-color",
                    payload = colorHex
                };
                await AzureIoTHub.SendMessageAsync(JsonConvert.SerializeObject(hubMessage));
            });
            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($@"Now look how I work my IoT magic using the accent color of this image... (holidayspirit)"));
            
        }
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}