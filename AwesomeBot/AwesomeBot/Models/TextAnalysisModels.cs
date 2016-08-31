using System.Collections.Generic;

namespace AwesomeBot.Models
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
    public class BatchResult
    {
        public DocumentResult[] documents { get; set; }
    }
    public class DocumentResult
    {
        public double score { get; set; }
        public string id { get; set; }
    }
}
