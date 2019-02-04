using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Extractor.Models;

namespace Extractor.Processors
{
    /*
        This class exists to provide basic functionality to all the various
        extractor classes that might get built for this project.
     */
    public abstract class BaseProcessor
    {
        // Directory on the local system for easy reading
        protected const string LocalResourcePath = @"C:\Projects\LocalResources\";
        // Directory on the external hard drive for the serious processing
        protected const string ResourcePath = @"E:\Analysis\Data\Reddit\";

        // The following three variables let us configure the ways in which our
        // code processes our data.
        protected string _startYear { get; set; }
        protected string _endYear { get; set; }
        protected int _chunkSize { get; set; }
        protected int _maxThreads { get; set; }

        // Turns out Windows has a series of ancient forbidden files that one does not create.
        private static readonly List<string> ForbiddenFiles = new List<string>
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM0",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT0",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        /*
        Gets some links for each source file
        */
        public Dictionary<string, List<StreamReader>> GetFiles()
        {
            var years = Directory.GetDirectories(ResourcePath);

            var result = new Dictionary<string, List<StreamReader>>();
            bool start = false;
            foreach (var year in years)
            {
                if (!string.IsNullOrEmpty(_endYear) && year.Contains(_endYear))
                {
                    break;
                }

                if (string.IsNullOrEmpty(_startYear))
                {
                    start = true;
                }

                if (!string.IsNullOrEmpty(_startYear) && year.Contains(_startYear))
                {
                    start = true;
                }

                if (!start)
                {
                    continue;
                }

                var rawDirectory = year + @"\Raw";
                if (!Directory.Exists(rawDirectory))
                {
                    Console.WriteLine(year + " has no RAW directory.");
                    continue;
                }

                var files = Directory.GetFiles(rawDirectory);

                var streams = new List<StreamReader>();
                foreach (var file in files)
                {
                    var yearComponents = file.Split('\\');
                    var yearName = yearComponents[yearComponents.Length - 3];

                    result.TryAdd(yearName, streams);

                    streams.Add(
                        File.OpenText(file)
                    );
                }
            }

            return result;
        }

        // Gets a chunk of the given source file for processing
        public string GetChunk(StreamReader stream)
        {
            var lines = new List<string>();

            string line;
            int i = 0;
            while (i++ < _chunkSize && (line = stream.ReadLine()) != null)
            {
                lines.Add(line);
            }

            if (!lines.Any())
            {
                return null;
            }

            return "[" + string.Join(",", lines) + "]";
        }

        private SemaphoreSlim _semaphore = null;

        // Inovked by an inheriting object, this runs our processing methods
        public List<ProcessorResult> Setup(Action<string, ProcessorResult> executor)
        {
            var years = GetFiles();

            var i = 0;
            using (_semaphore = new SemaphoreSlim(_maxThreads))
            {
                foreach (var yearRecord in years)
                {
                    var yearlyResult = new ProcessorResult
                    {
                        Name = yearRecord.Key,
                        WordCountBySub = new Dictionary<string, Dictionary<string, int>>(),
                        UniqueUsers = new Dictionary<string, int>(),
                        UniqueUsersBySub = new Dictionary<string, Dictionary<string, int>>()
                    };

                    foreach (var file in yearRecord.Value)
                    {
                        var fileTimer = new Stopwatch();
                        fileTimer.Start();

                        i++;
                        Console.WriteLine("Processing file " + i);

                        var fileResult = new ProcessorResult()
                        {
                            Name = yearlyResult.Name,
                            WordCountBySub = new Dictionary<string, Dictionary<string, int>>(),
                            UniqueUsers = new Dictionary<string, int>(),
                            UniqueUsersBySub = new Dictionary<string, Dictionary<string, int>>()
                        };

                        var tasks = new List<Task>();

                        string chunk;
                        while ((chunk = GetChunk(file)) != null)
                        {
                            // Clear out the task queue every so often to conserve memory
                            if (_taskTracker % 500 == 0)
                            {
                                Task.WaitAll(tasks.ToArray());
                                Console.WriteLine("Clearing task result backlog");
                                tasks.Clear();
                            }

                            _semaphore.Wait();

                            var task = BuildProcessorTask(chunk, executor, fileResult, yearlyResult.Name);

                            tasks.Add(task);
                        }

                        Task.WaitAll(tasks.ToArray());
                        fileTimer.Stop();
                        Console.WriteLine("Processing Time: " + fileTimer.Elapsed);

                        file.Close();

                        var wrappedFileResult = new List<ProcessorResult> 
                        {
                            fileResult
                        };

                        ConsolidateResults(yearlyResult, wrappedFileResult);
                    }

                    CompileTotalCount(yearlyResult);

                    var currentResult = new List<ProcessorResult> 
                    {
                        yearlyResult
                    };

                    LogResults(currentResult);
                }
            }

            return null;
        }

        // Lock object for keeping our results thread-safe
        private static object _lock = new object();
        private int _taskTracker = 0;

        private Task BuildProcessorTask(string chunk, Action<string, ProcessorResult> executor, ProcessorResult fileResult, string name)
        {
            var tempChunk = chunk;
            _taskTracker++;
            var taskId = _taskTracker;

            var task = new Task(() =>
            {
                var result = new ProcessorResult
                {
                    Name = name,
                    WordCountBySub = new Dictionary<string, Dictionary<string, int>>(),
                    UniqueUsers = new Dictionary<string, int>(),
                    UniqueUsersBySub = new Dictionary<string, Dictionary<string, int>>()
                };

                executor.Invoke(tempChunk, result);

                var results = new[]
                {
                    result
                }.ToList();

                try
                {
                    lock (_lock)
                    {
                        ConsolidateResults(fileResult, results);
                    }

                    if (taskId % 2000 == 0)
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                    }

                    Console.WriteLine("End Thread " + taskId);
                }
                finally
                {
                    if (_semaphore != null)
                    {
                        _semaphore.Release();
                    }
                }
            });

            task.Start();

            return task;
        }

        // This takes a list of result objects and consolidates them into one.
        // This keeps us from having a nasty memory leak.
        public void ConsolidateResults(ProcessorResult final, List<ProcessorResult> results)
        {
            if (final.UniqueUsers == null)
            {
                final.UniqueUsers = new Dictionary<string, int>();
            }

            if (final.UniqueUsersBySub == null)
            {
                final.UniqueUsersBySub = new Dictionary<string, Dictionary<string, int>>();
            }

            foreach (var node in results)
            {
                foreach (var sub in node.WordCountBySub)
                {
                    final.WordCountBySub.TryAdd(sub.Key, new Dictionary<string, int>());

                    foreach (var word in node.WordCountBySub[sub.Key])
                    {
                        final.WordCountBySub[sub.Key].TryAdd(word.Key, 0);
                        final.WordCountBySub[sub.Key][word.Key] += word.Value;
                    }
                }

                foreach (var user in node.UniqueUsers)
                {
                    final.UniqueUsers.TryAdd(user.Key, 0);
                    final.UniqueUsers[user.Key] += user.Value;
                }

                foreach (var sub in node.UniqueUsersBySub)
                {
                    final.UniqueUsersBySub.TryAdd(sub.Key, new Dictionary<string, int>());

                    foreach (var user in sub.Value)
                    {
                        final.UniqueUsersBySub[sub.Key].TryAdd(user.Key, 0);
                        final.UniqueUsersBySub[sub.Key][user.Key] += user.Value;
                    }
                }
            }
        }

        private void CompileTotalCount(ProcessorResult result)
        {
            result.WordCounts = new Dictionary<string, long>();

            foreach (var sub in result.WordCountBySub)
            {
                foreach (var word in sub.Value)
                {
                    // Populate word counts
                    result.WordCounts.TryAdd(word.Key, 0);
                    result.WordCounts[word.Key] += word.Value;
                }
            }
        }

        // Writes all the data in a result object to some files for use in Stata
        protected void LogResults(List<ProcessorResult> results)
        {
            foreach (var result in results)
            {
                // Write our word counts to a file with the provided name (usually a year).
                Console.WriteLine("Writing word list for " + result.Name);

                var wordList = result.WordCounts
                    .ToList()
                    .OrderByDescending(word => word.Value);

                using (FileStream file = File.OpenWrite(string.Format("{0}{1}.csv", LocalResourcePath, result.Name)))
                {
                    foreach (var word in wordList)
                    {
                        if (word.Value <= 5)
                        {
                            continue;
                        }

                        var text = string.Format("{0},{1}\r\n", word.Key, word.Value);
                        file.Write(Encoding.Unicode.GetBytes(text));
                    }
                }

                // Create file for our user data
                Console.WriteLine("Writing user list for " + result.Name);
                
                using (FileStream file = File.OpenWrite(string.Format("{0}{1} - Users.csv", LocalResourcePath, result.Name)))
                {
                    foreach (var user in result.UniqueUsers)
                    {
                        var text = string.Format("{0},{1}\r\n", user.Key, user.Value);
                        file.Write(Encoding.Unicode.GetBytes(text));
                    }
                }

                // Create a folder for our subreddit data
                Console.WriteLine("Writing SubReddit-specific word lists for " + result.Name);

                var namedDirectory = LocalResourcePath + result.Name;

                Console.WriteLine("Creating directory " + namedDirectory);
                Directory.CreateDirectory(namedDirectory);

                foreach (var sub in result.WordCountBySub)
                {
                    var subWords = sub.Value
                        .ToList()
                        .OrderByDescending(word => word.Value);
                    
                    var invalidChars = Path.GetInvalidFileNameChars();
                    var cleanName = sub.Key;
                    foreach (var invalid in invalidChars)
                    {
                        cleanName.Replace(invalid.ToString(), "_");
                    }

                    if (ForbiddenFiles.Contains(cleanName.ToUpper()))
                    {
                        cleanName += " (forbidden)";
                    }

                    using (FileStream file = File.OpenWrite(
                        string.Format("{0}\\{1}.csv", namedDirectory, cleanName)
                    ))
                    {
                        foreach (var word in subWords)
                        {
                            if (word.Value <= 5)
                            {
                                continue;
                            }
                            
                            var text = string.Format("{0},{1}\r\n", word.Key, word.Value);
                            file.Write(Encoding.Unicode.GetBytes(text));
                        }
                    }
                }

                // Create our user-to-sub data
                Console.WriteLine("Creating user comments by subreddit data");

                foreach (var sub in result.UniqueUsersBySub)
                {
                    var invalidChars = Path.GetInvalidFileNameChars();
                    var cleanName = sub.Key;
                    foreach (var invalid in invalidChars)
                    {
                        cleanName.Replace(invalid.ToString(), "_");
                    }

                    if (ForbiddenFiles.Contains(cleanName.ToUpper()))
                    {
                        cleanName += " (forbidden)";
                    }

                    using (FileStream file = File.OpenWrite(
                        string.Format("{0}\\{1} - Users.csv", namedDirectory, cleanName)
                    ))
                    {
                        foreach (var user in sub.Value)
                        {
                            var text = string.Format("{0},{1}\r\n", user.Key, user.Value);
                            file.Write(Encoding.Unicode.GetBytes(text));
                        }
                    }
                }
            }

            Console.WriteLine("Done!");
        }
    }
}
