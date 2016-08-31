using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AwesomeBot.Controllers
{
    public class WebhooksController : ApiController
    {
        #region WebHook
        public class WebHookMessage
        {
            public string Type { get; set; }
            public dynamic Data { get; set; }
        }

        private string resolveEmoji(string emotion)
        {
            switch (emotion)
            {
                case "Anger":
                    return ":@";
                case "Contempt":
                    return "(mm)";
                case "Disgust":
                    return "(puke)";
                case "Fear":
                    return ":S";
                case "Happiness":
                    return "(happy)";
                case "Sadness":
                    return ":(";
                case "Surprise":
                    return ":o";
            }
            return "(wtf)";
        }

        
        public async Task<IHttpActionResult> Post([FromBody]WebHookMessage webHookMessage)
        {
            if (webHookMessage.Type == "EmotionUpdate")
            {

                var text = resolveEmoji(webHookMessage.Data);

                // const string from = "28:b8f1bfb1-0e49-4ba1-a05c-68a97a367efe";
                var channelBotId = ConfigurationManager.AppSettings["Skype:Channel:BotId"];
                var channelUserId = ConfigurationManager.AppSettings["Skype:Channel:UserId"];
                // const string to = "29:1lO9nnPO3DjKo8-TU6t0A2uU-qt0l8PvAl5uC-EO6gBw";
                using (var client = new ConnectorClient(new Uri("https://skype.botframework.com")))
                {

                    var ConversationId = await client.Conversations.CreateDirectConversationAsync(new ChannelAccount(channelBotId), new ChannelAccount(channelUserId));
                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = new ChannelAccount(channelBotId);
                    message.Recipient = new ChannelAccount(channelUserId);
                    message.Conversation = new ConversationAccount(id: ConversationId.Id);
                    message.Text = text;
                    await client.Conversations.SendToConversationAsync((Activity)message);

                    //await client.Messages.SendMessageAsync(outMessage);
                }
            }
            return Ok();
        }
        #endregion
    }
}
