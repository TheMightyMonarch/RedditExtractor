using System;
using System.Collections.Generic;
using System.IO;
using Extractor.Models;

namespace Extractor.Processors
{
    public interface IProcessor
    {
        void Process(string chunk, ProcessorResult result);
    }
}
