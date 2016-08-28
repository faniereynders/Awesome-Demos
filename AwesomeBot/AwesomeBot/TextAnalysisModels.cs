using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeBot
{
    public class BatchInput
    {
        public List<DocumentInput> documents { get; set; }
    }
    public class DocumentInput
    {
        public double id { get; set; }
        public string text { get; set; }
    }

    // Classes to store the result from the sentiment analysis
    public class BatchResult
    {
        public List<DocumentResult> documents { get; set; }
    }
    public class DocumentResult
    {
        public double score { get; set; }
        public string id { get; set; }
    }
}
