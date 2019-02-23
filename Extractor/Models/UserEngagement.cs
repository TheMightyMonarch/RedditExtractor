using System.Collections.Generic;

namespace Extractor.Models
{
    public class UserEngagement
    {
        public string Username { get; set; }
        public long TotalComments { get; set; }
        public Dictionary<string, float> CommunityProportions { get; set; }
    }
}