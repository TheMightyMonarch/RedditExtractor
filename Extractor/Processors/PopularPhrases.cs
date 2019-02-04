using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Extractor.Models;
using Jil;

namespace Extractor.Processors
{
    public class PopularPhrases : BaseProcessor
    {
        public PopularPhrases()
        {
            _startYear = "2005";
            _endYear = "2018";
            _chunkSize = 25000;
            _maxThreads = 8;
        }

        public void Setup()
        {
            
        }

        public void Process(string chunk, ProcessorResult result)
        {
            var comments = JSON.Deserialize<Comment[]>(chunk);
            
            // We'll get phrases of up to ten words
            var lastWords = new FixedSizeQueue<string>(10);
            foreach (var comment in comments)
            {
                if (!CommunityWhitelist.Values.Contains(comment.subreddit.ToUpper()))
                {
                    continue;
                }

                var body = Regex.Replace(comment.body, @"[.!?,_]", " ");
                var tokens = body.Split((string[]) null, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .Select(token => token.ToUpper());

                result.WordCountBySub.TryAdd(comment.subreddit, new Dictionary<string, int>());

                foreach (var token in tokens)
                {
                    lastWords.Enqueue(token);
                    if (lastWords.Count < 2)
                    {
                        continue;
                    }

                    var words = lastWords.ToArray();

                    // Join our phrase together
                    var phrase = string.Join(" ", words);

                    result.WordCountBySub[comment.subreddit].TryAdd(phrase, 0);
                    result.WordCountBySub[comment.subreddit][phrase] += 1;
                }

                // After we're done with a comment, clear out the queue
                lastWords.Clear();
            }
        }
    }
}
