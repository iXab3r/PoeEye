using System.Text.RegularExpressions;
using Guards;
using PoeShared.Common;

namespace PoeEye.ItemParser.Services
{
    internal struct PoeModParser
    {
        public PoeModParser(IPoeItemMod mod, Regex matchingRegex)
        {
            Guard.ArgumentNotNull(mod, nameof(mod));
            Guard.ArgumentNotNull(matchingRegex, nameof(matchingRegex));

            Mod = mod;
            MatchingRegex = matchingRegex;
        }

        public IPoeItemMod Mod { get; }

        public Regex MatchingRegex { get; }
    }
}
