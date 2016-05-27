using System.Text.RegularExpressions;

namespace PoePricer.Parser
{
    public struct AffixLineSource
    {
        public Regex AffixRegExp;
        public string[] AffixLineArgs;
        public string[] ArgMods;
    }
}