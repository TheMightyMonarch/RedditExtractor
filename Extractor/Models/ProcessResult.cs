using System.Collections.Generic;

namespace Extractor.Models
{
    public class ProcessorResult
    {
        public string Name { get; set; }

        public Dictionary<string, long> WordCounts { get; set; }

        public Dictionary<string, Dictionary<string, int>> WordCountBySub { get; set; }

        public Dictionary<string, int> UniqueUsers { get; set; }

        public Dictionary<string, Dictionary<string, int>> UniqueUsersBySub { get; set; }
    }
}
