using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot
{

    public class RSVPResponse
    {
        public long created { get; set; }
        public int duration { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public int rsvp_limit { get; set; }
        public Rsvp_Sample[] rsvp_sample { get; set; }
    }

    public class Rsvp_Sample
    {
        public int id { get; set; }
        public long created { get; set; }
        public long updated { get; set; }
        public Member member { get; set; }
    }

    public class Member
    {
        public int id { get; set; }
        public string name { get; set; }
        public Photo photo { get; set; }
        public Event_Context event_context { get; set; }
    }

    public class Photo
    {
        public int id { get; set; }
        public string highres_link { get; set; }
        public string photo_link { get; set; }
        public string thumb_link { get; set; }
    }

    public class Event_Context
    {
        public bool host { get; set; }
    }

}
