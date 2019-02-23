using System.Collections.Generic;

namespace Extractor.Models
{
    public class ProcessorResult
    {
        public string Name { get; set; }

        public Dictionary<string, long> WordCounts { get; set; }

        public Dictionary<string, Dictionary<string, long>> WordCountBySub { get; set; }

        public Dictionary<string, long> UniqueUsers { get; set; }

        public Dictionary<string, Dictionary<string, long>> UniqueUsersBySub { get; set; }
    }
}
