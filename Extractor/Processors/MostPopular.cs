using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using Extractor.Models;
using Jil;

namespace Extractor.Processors
{
    public class MostPopular : BaseProcessor,
        IProcessor
    {
        // Set our parameters
        // TODO: let us define these from the console
        public MostPopular()
        {
            _startYear = "2005";
            _endYear = "2018";
            _chunkSize = 25000;
            _maxThreads = 12;
        }

        // Start the process!
        public void Setup()
        {
            base.Setup(Process);
        }

        private static readonly List<string> _excludedWords = new List<string>
        {
            "THE",
            "TO",
            "AND",
            "OF",
            "IT",
            "THAT",
            "IN",
            "IS",
            "FOR",
            "WAS",
            "ON",
            "WITH",
            "HAVE",
            "BUT",
            "NOT",
            "BE",
            "THIS",
            "ARE",
            "IF"
        };

        /*
            This does a few things for us:
            
            1. Processes all the comments in a given chunk
            2. Gets word counts for every word in every comment
            3. Categorizes these word counts by SubReddit
            
            Note: "result" is modified in place
         */
        public void Process(string chunk, ProcessorResult result)
        {
            var comments = JSON.Deserialize<Comment[]>(chunk);
 
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
                    if (token.Length < 2)
                    {
                        continue;
                    }

                    // Populate word count by subreddit
                    result.WordCountBySub[comment.subreddit].TryAdd(token, 0);
                    result.WordCountBySub[comment.subreddit][token] += 1;
                }

                result.WordCountBySub[comment.subreddit].TryAdd("+++ Total Count +++", 0);
                result.WordCountBySub[comment.subreddit]["+++ Total Count +++"] += 1;

                result.UniqueUsers.TryAdd(comment.author, 0);
                result.UniqueUsers[comment.author] += 1;

                result.UniqueUsersBySub.TryAdd(comment.subreddit.ToUpper(), new Dictionary<string, int>());

                result.UniqueUsersBySub[comment.subreddit.ToUpper()].TryAdd(comment.author, 0);
                result.UniqueUsersBySub[comment.subreddit.ToUpper()][comment.author] += 1;
            }
        }
    }
}
