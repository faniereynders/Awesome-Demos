﻿using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
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
                case "Angry":
                    return ":@";
                case "Contempt":
                    return "(mm)";
                case "Disgusted":
                    return "(puke)";
                case "Fearful":
                    return ":S";
                case "Happy":
                    return "(happy)";
                case "Sad":
                    return ":(";
                case "Surprised":
                    return ":o";
            }
            return "(wtf)";
        }

        
        public async Task<IHttpActionResult> Post([FromBody]WebHookMessage webHookMessage)
        {
            if (webHookMessage.Type == "EmotionUpdate")
            {

                var text = resolveEmoji(webHookMessage.Data);

                using (var client = new ConnectorClient(new Uri("https://facebook.botframework.com")))
                {
                    
                    var ConversationId = await client.Conversations.CreateDirectConversationAsync(new ChannelAccount("1145696458786995","awesomebot"), new ChannelAccount("1351063284908341","Fanie Reynders"));
                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = new ChannelAccount("1145696458786995","awesomebot");
                    message.Recipient = new ChannelAccount() { Name = "Fanie Reynders", Id= "1351063284908341" };
                    message.Conversation = new ConversationAccount(id: ConversationId.Id);
                    message.Text = text;
                    await client.Conversations.SendToConversationAsync((Activity)message);

                    //await client.Messages.SendMessageAsync(outMessage);
                }

                using (var client = new ConnectorClient(new Uri("https://skype.botframework.com")))
                {

                    var ConversationId = await client.Conversations.CreateDirectConversationAsync(new ChannelAccount("28:8bab1b35-e9cc-4904-8d4c-2051fd5be4b3"), new ChannelAccount("29:1LDLrwv22HJ27BvroovzI7HujTyqOeYuTKt58KUVQU54"));
                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = new ChannelAccount("28:8bab1b35-e9cc-4904-8d4c-2051fd5be4b3");
                    message.Recipient = new ChannelAccount("29:1LDLrwv22HJ27BvroovzI7HujTyqOeYuTKt58KUVQU54");
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
