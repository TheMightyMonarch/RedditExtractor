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
        public const string BraIncels = "Braincels";
        public const string TheGreatAwakening = "The_GreatAwakening";
        public const string MaleForeverAlone = "MaleForeverAlone";
        public const string IncelReddit = "IncelReddit";
        public const string IncelsWithoutHate = "IncelsWithoutHate";
        public const string DebateAltRight = "DEBATEALTRIGHT";
        public const string Fascist = "FASCIST";
        public const string BeholdTheMasterRace = "BEHOLDTHEMASTERRACE";
        public const string CringeAnarchy = "CRINGEANARCHY";

        public static readonly List<string> Values = new List<string>
        {
            MensRights,
            TheRedPill,
            Mgtow,
            Incels,
            BraIncels,
            BeatingWomen,
            Quincels,
            MaleForeverAlone,
            IncelReddit,
            IncelsWithoutHate,

            GreatAwakening,
            TheGreatAwakening,
            Conservative,
            DebateFascism,
            TheDonald,
            Conspiracy,
            DebateAltRight,
            Fascist,
            BeholdTheMasterRace,
            CringeAnarchy
        };
    }
}
