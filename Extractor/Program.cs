using System;
using Extractor.PostProcessors;
using Extractor.Processors;

namespace Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor = new MostPopular();

            processor.Setup();
        }
    }
}
