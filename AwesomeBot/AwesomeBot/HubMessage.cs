using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot
{
    public class HubMessage
    {
        public string Command { get; set; }
        public dynamic Payload { get; set; }
    }
}
