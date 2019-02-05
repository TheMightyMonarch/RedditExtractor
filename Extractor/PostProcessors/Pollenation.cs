using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extractor.Models;

namespace Extractor.PostProcessors
{
    public class Engagement
    {
        protected const string LocalResourcePath = @"C:\Projects\LocalResources\";
        protected const string ResourcePath = @"F:\Analysis\Data\Reddit\";

        // TODO - TBD if we need to retrieve this data from files to conserve memory
        public static void DoPostProcessing(List<ProcessorResult> records)
        {
            var engagement = GetUserEngagement(records);
            GetAverageCommunityEngagement(engagement, records.First().Name);
        }

        // Derives the rates of cross-Engagement between users
        // of the targeted communities.
        public static List<UserEngagement> GetUserEngagement(List<ProcessorResult> records)
        {
            Console.WriteLine("User Engagement review");
            
            if (records == null)
            {
                Console.WriteLine("No records present for user Engagement review");
                return new List<UserEngagement>();
            }
            
            var results = new List<UserEngagement>();

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
                        UserEngagement result = new UserEngagement
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

                        results.Add(result);
                    }
                }
            }

            return results;
        }

        // Determines the average rate at which each community
        // cross-pollenates with other communities
        public static void GetAverageCommunityEngagement(List<UserEngagement> engagement, string name)
        {
            var averageEngagement = new UserEngagement
            {
                Username = "Average Engagement",
                TotalComments = 0,
                CommunityProportions = new Dictionary<string, float>()
            };

            var totalAdjusted = new Dictionary<string, int>();
            var adjustedEngagement = new UserEngagement
            {
                Username = "Average Engagement",
                TotalComments = 0,
                CommunityProportions = new Dictionary<string, float>()
            };

            foreach (var user in engagement)
            {
                foreach (var node in user.CommunityProportions)
                {
                    averageEngagement.CommunityProportions.TryAdd(node.Key, 0);
                    averageEngagement.CommunityProportions[node.Key] += node.Value;

                    if (node.Value > 0)
                    {
                        totalAdjusted.TryAdd(node.Key, 0);
                        totalAdjusted[node.Key] += 1;
                        adjustedEngagement.CommunityProportions.TryAdd(node.Key, 0);
                        adjustedEngagement.CommunityProportions[node.Key] += node.Value;
                    }
                }
            }

            var keys = averageEngagement.CommunityProportions.Keys.ToList();
            foreach (var key in keys)
            {
                averageEngagement.CommunityProportions[key] /= engagement.Count;
            }

            using (var file = File.OpenWrite(string.Format("{0}{1} - Average Engagement.csv", LocalResourcePath, name)))
            {
                foreach (var community in averageEngagement.CommunityProportions)
                {
                    var text = string.Format("{0},{1}\r\n", community.Key, community.Value.ToString("0.000"));
                    file.Write(Encoding.Unicode.GetBytes(text));
                }
            }

            keys = adjustedEngagement.CommunityProportions.Keys.ToList();
            foreach (var key in keys)
            {
                adjustedEngagement.CommunityProportions[key] /= totalAdjusted[key];
            }

            using (var file = File.OpenWrite(string.Format("{0}{1} - Adjusted Average Engagement.csv", LocalResourcePath, name)))
            {
                foreach (var community in adjustedEngagement.CommunityProportions)
                {
                    var text = string.Format("{0},{1}\r\n", community.Key, community.Value.ToString("0.000"));
                    file.Write(Encoding.Unicode.GetBytes(text));
                }
            }
        }
    }
}