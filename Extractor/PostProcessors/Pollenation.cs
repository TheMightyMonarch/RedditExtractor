using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extractor.Models;

namespace Extractor.PostProcessors
{
    public class Pollenation
    {
        protected const string LocalResourcePath = @"C:\Projects\LocalResources\";
        protected const string ResourcePath = @"F:\Analysis\Data\Reddit\";

        // TODO - TBD if we need to retrieve this data from files to conserve memory
        public static void DoPostProcessing(List<ProcessorResult> records)
        {
            var executors = new List<Func<List<ProcessorResult>, bool>>
            {
                UserPollenation,
                CommunityPollenation
            };

            foreach (var executor in executors)
            {
                executor.Invoke(records);
            }
        }

        // Derives the rates of cross-pollenation between users
        // of the targeted communities.
        public static bool UserPollenation(List<ProcessorResult> records)
        {
            Console.WriteLine("User pollenation review");
            
            if (records == null)
            {
                Console.WriteLine("No records present for user pollenation review");
                return false;
            }

            var whitelist = new List<string>();
            whitelist.AddRange(CommunityWhitelist.Values);
            whitelist.Sort();

            foreach (var record in records)
            {
                using (var file = File.OpenWrite(string.Format("{0}{1} - User Engagement.csv", LocalResourcePath, record.Name)))
                {
                    var headerText = new List<string>
                    {
                        "Username",
                        "Total Comments"
                    };

                    headerText.AddRange(whitelist);

                    file.Write(Encoding.Unicode.GetBytes(string.Join(",", headerText) + "\r\n"));

                    // Determine community engagement of each user for each community
                    foreach (var user in record.UniqueUsers)
                    {
                        UserPollenation result = new UserPollenation
                        {
                            Username = user.Key,
                            TotalComments = user.Value,
                            CommunityProportions = new Dictionary<string, float>()
                        };

                        // Get our users' community engagement
                        foreach (var community in whitelist)
                        {
                            if (record.UniqueUsersBySub.ContainsKey(community) 
                                && record.UniqueUsersBySub[community].ContainsKey(result.Username))
                            {
                                var total = (float) result.TotalComments;
                                var subset = (float) record.UniqueUsersBySub[community][result.Username];

                                var engagement = (subset / total) * 100;

                                result.CommunityProportions.Add(community, engagement);
                            }
                            else
                            {
                                result.CommunityProportions.Add(community, 0);
                            }
                        }

                        // Log out our engagement
                        var text = new List<string>
                        {
                            result.Username,
                            result.TotalComments.ToString()
                        };

                        foreach (var community in whitelist)
                        {
                            text.Add(result.CommunityProportions[community].ToString("0.000"));
                        }

                        file.Write(Encoding.Unicode.GetBytes(string.Join(",", text) + "\r\n"));
                    }
                }
            }

            return true;
        }

        // Determines the average rate at which each community
        // cross-pollenates with other communities
        public static bool CommunityPollenation(List<ProcessorResult> records)
        {
            return true;
        }
    }
}