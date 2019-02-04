using System.Runtime.Serialization;

namespace Extractor.Models
{
    public class Comment
    {
        public string author { get; set; }

        public string body { get; set; }

        public string subreddit { get; set; }
    }
}
