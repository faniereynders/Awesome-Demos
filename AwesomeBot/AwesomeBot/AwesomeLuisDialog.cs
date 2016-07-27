using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            var message = $"Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
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
                activity.Attachments = new List<Attachment>() { new Attachment(contentType:"image/jpeg", contentUrl: winner.photo.photo_link) };
               // activity.Recipient = context.

                
                
                await context.PostAsync(activity);
            }
            
            context.Wait(MessageReceived);
        }

    }
}
