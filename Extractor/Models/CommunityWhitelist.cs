using System.Collections.Generic;

namespace Extractor.Models
{
    public class CommunityWhitelist
    {
        public const string MensRights = "MENSRIGHTS";
        public const string TheRedPill = "THEREDPILL";
        public const string Incels = "INCELS";
        public const string Mgtow = "MGTOW";
        public const string TheDonald = "THE_DONALD";
        public const string BeatingWomen = "BEATINGWOMEN";
        public const string Politics = "POLITICS";
        public const string News = "NEWS";
        public const string Conservative = "CONSERVATIVE";
        public const string DebateFascism = "DEBATEFASCISM";
        public const string GreatAwakening = "GREATAWAKENING";
        public const string Feminism = "FEMINISM";
        public const string ShitRedditSays = "SHITREDDITSAYS";
        public const string Conspiracy = "CONSPIRACY";
        public const string Quincels = "QUINCELS";

        public static readonly List<string> Values = new List<string>
        {
            MensRights,
            TheRedPill,
            Incels,
            Mgtow,
            TheDonald,
            BeatingWomen,
            Politics,
            News,
            Conservative,
            DebateFascism,
            GreatAwakening,
            Feminism,
            ShitRedditSays,
            Conspiracy,
            Quincels
        };
    }
}
