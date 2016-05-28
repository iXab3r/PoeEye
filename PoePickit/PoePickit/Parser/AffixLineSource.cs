using System.Text.RegularExpressions;

namespace PoePricer.Parser
{
    internal struct AffixLineSource
    {
        public Regex AffixRegExp;
        public string[] AffixLineArgs;
        public string[] ArgMods;
    }
}