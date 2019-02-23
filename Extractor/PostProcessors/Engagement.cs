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
            GetLanguageOverlap(records);
            var engagement = GetUserEngagement(records);
            GetAverageCommunityEngagement(engagement, records.First().Name);
            GetCommunityOverlap(engagement, records.First().Name);
        }

        public static void GetLanguageOverlap(List<ProcessorResult> records)
        {
            foreach (var record in records)
            {
                var wordSet = new HashSet<string>();
                var masculinistSet = new HashSet<string>();
                var altrightSet = new HashSet<string>();
                var overlapSet = new HashSet<string>();

                foreach (var community in record.WordCountBySub)
                {
                    // Loop through each word
                    foreach (var word in community.Value)
                    {
                        wordSet.Add(word.Key);
                        
                        var overlap = false;

                        if (CommunityWhitelist.Masculinist.Contains(community.Key.ToUpper()))
                        {
                            masculinistSet.Add(word.Key);
                            overlap = ScanOverlapCommunities(word.Key, CommunityWhitelist.AltRight, record.WordCountBySub);
                        }
                        else if (CommunityWhitelist.AltRight.Contains(community.Key.ToUpper()))
                        {
                            altrightSet.Add(word.Key);
                            overlap = ScanOverlapCommunities(word.Key, CommunityWhitelist.Masculinist, record.WordCountBySub);
                        }
                        else
                        {
                            throw new Exception("Community not in whitelists: '" + community.Key.ToUpper() + "'");
                        }

                        if (overlap)
                        {
                            overlapSet.Add(word.Key);
                        }
                    }
                }

                var totalWords = wordSet.Count();
                var totalMasculinist = masculinistSet.Count();
                var totalAltright = altrightSet.Count();
                var totalOverlap = overlapSet.Count();

                var text = "Total Words,Total Masculinist Words,Total Alt-Right Words,Total Overlap Words\r\n";
                using (var file = File.OpenWrite(string.Format("{0}{1} - Language Overlap.csv", LocalResourcePath, record.Name)))
                {
                    text += string.Format("{0},{1},{2},{3}",
                        totalWords,
                        totalMasculinist,
                        totalAltright,
                        totalOverlap
                    );
                    
                    file.Write(Encoding.Unicode.GetBytes(text));
                }
            }
        }

        private static bool ScanOverlapCommunities(string word, List<string> communities, Dictionary<string, Dictionary<string, long>> record)
        {
            foreach (var community in communities)
            {
                var literalCommunity = record.Keys.FirstOrDefault(k => k.ToUpper() == community.ToUpper());
                if (literalCommunity == null)
                {
                    continue;
                }

                if (record[literalCommunity].ContainsKey(word))
                {
                    return true;
                }
            }
        
            return false;
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

                        var masculinistTotal = 0;
                        var altrightTotal = 0;

                        // Get our users' community engagement
                        foreach (var community in whitelist)
                        {
                            var engagement = (float) 0;

                            if (record.UniqueUsersBySub.ContainsKey(community) 
                                && record.UniqueUsersBySub[community].ContainsKey(result.Username))
                            {
                                var total = (float) result.TotalComments;
                                var subset = (float) record.UniqueUsersBySub[community][result.Username];

                                engagement = (subset / total) * 100;

                                if (CommunityWhitelist.Masculinist.Contains(community))
                                {
                                    masculinistTotal += (int) total;
                                }
                                else
                                {
                                    altrightTotal += (int) total;
                                }
                            }

                            result.CommunityProportions.Add(community, engagement);
                        }

                        var masculinistEngagement = (masculinistTotal / (float) (masculinistTotal + altrightTotal)) * 100;
                        var altrightEngagement = (altrightTotal / (float) (masculinistTotal + altrightTotal)) * 100;

                        result.CommunityProportions.Add("+++ Masculinist Communities +++", masculinistEngagement);
                        result.CommunityProportions.Add("+++ Protofascist Communities +++", altrightEngagement);

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

            var totalAdjusted = new Dictionary<string, long>();
            var adjustedEngagement = new UserEngagement
            {
                Username = "Average Engagement",
                TotalComments = 0,
                CommunityProportions = new Dictionary<string, float>()
            };

            var communityCommentCounts = new Dictionary<string, long>();
            var communityUserCounts = new Dictionary<string, long>();

            foreach (var user in engagement)
            {
                foreach (var node in user.CommunityProportions)
                {
                    if (node.Key.Contains("+++"))
                    {
                        continue;
                    }

                    averageEngagement.CommunityProportions.TryAdd(node.Key, 0);
                    averageEngagement.CommunityProportions[node.Key] += node.Value;

                    communityCommentCounts.TryAdd(node.Key, 0);
                    communityUserCounts.TryAdd(node.Key, 0);

                    if (node.Value > 0)
                    {
                        totalAdjusted.TryAdd(node.Key, 0);
                        totalAdjusted[node.Key] += 1;
                        adjustedEngagement.CommunityProportions.TryAdd(node.Key, 0);
                        adjustedEngagement.CommunityProportions[node.Key] += node.Value;

                        communityCommentCounts[node.Key] += (long) (user.TotalComments * (node.Value / 100));
                        communityUserCounts[node.Key] += 1;
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
                    var text = string.Format("{0},{1},{2},{3}\r\n", 
                        community.Key, 
                        communityCommentCounts[community.Key], 
                        communityUserCounts[community.Key], 
                        community.Value.ToString("0.000")
                    );

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

        public static void GetCommunityOverlap(List<UserEngagement> engagement, string name)
        {
            var total = 0;
            var masculinist = 0;
            var altright = 0;
            var both = 0;

            foreach (var user in engagement)
            {
                total++;

                var userMasculinist = 0;
                var userAltright = 0;

                foreach (var community in user.CommunityProportions)
                {
                    if (community.Key.Contains("+++"))
                    {
                        continue;
                    }

                    if (community.Value > 0)
                    {
                        if (CommunityWhitelist.Masculinist.Contains(community.Key))
                        {
                            userMasculinist++;
                        }
                        else
                        {
                            userAltright++;
                        }
                    }
                }

                if (userMasculinist > 0 && userAltright > 0)
                {
                    masculinist++;
                    altright++;
                    both++;
                }
                else if (userMasculinist > 0)
                {
                    masculinist++;
                }
                else if (userAltright > 0)
                {
                    altright++;
                }
            }

            if (total == 0)
            {
                return;
            }

            using (var file = File.OpenWrite(string.Format("{0}{1} - Cross Over.csv", LocalResourcePath, name)))
            {
                file.Write(Encoding.Unicode.GetBytes(
                    "Total Users, Overlap Users, Overlap %, Masculinist Users, Masculinist %, Proto-Fascist Users, Proto-Fascist %\r\n"));

                var text = string.Format("{0},{1},{2},{3},{4},{5},{6}",
                    total,
                    both,
                    ((both / total) * 100).ToString("0.000"),
                    masculinist,
                    ((masculinist / total) * 100).ToString("0.000"),
                    altright,
                    ((altright / total) * 100).ToString("0.000")
                );

                file.Write(Encoding.Unicode.GetBytes(text));
            }
        }
    }
}