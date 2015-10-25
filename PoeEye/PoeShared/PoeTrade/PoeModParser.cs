namespace PoeShared.PoeTrade
{
    using System.Text.RegularExpressions;

    using Common;

    using Guards;

    internal struct PoeModParser
    {
        public PoeModParser(IPoeItemMod mod, Regex matchingRegex)
        {
            Guard.ArgumentNotNull(() => mod);
            Guard.ArgumentNotNull(() => matchingRegex);
            
            Mod = mod;
            MatchingRegex = matchingRegex;
        }

        public IPoeItemMod Mod { get; }

        public Regex MatchingRegex { get; }
    }
}