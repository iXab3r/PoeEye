using System.Text.RegularExpressions;
using Guards;
using PoeShared.Common;

namespace PoeEye.ItemParser
{
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