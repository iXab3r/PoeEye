using System;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PoePricer.Parser
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Extensions;

    public enum AffixTypes
    {
        prefixes,
        suffixes,
        implicitaff,
        affixes
    };

    public struct AffixLineSource
    {
        public Regex AffixRegExp;
        public string[] AffixLineArgs;
        public string[] ArgMods;
    }


    public class AffixesSource : PricerDataReader
    {
        public AffixLineSource[] AffixesLines { get; set; }

        public AffixesSource(string fileName, IDictionary<ParseRegEx, Regex> knownRegexes)
            : base(Path.Combine("Affixes", fileName))
        {
            AffixesLines = Read(fileName, knownRegexes);
        }

        private AffixLineSource[] Read(string fileName, IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            var lines = RawLines;

            var result = new List<AffixLineSource>();

            foreach (var line in lines)
            {
                if (line.StartsWith(";") || line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

                Regex parseRegex;
                knownRegexes.TryGetValue(ParseRegEx.RegexAffixFileLine, out parseRegex);
                var match = parseRegex.Match(line);

                if (match.Success)
                {
                    var tRawRegex = match.Groups["affixRegexpPart"].Value;
                    var affixLineSource = new AffixLineSource
                    {
                        AffixRegExp = new Regex(tRawRegex, RegexOptions.Compiled)
                    };

                    if (match.Groups["affixArgsPart"].Success)
                    {
                        var linePart = match.Groups["affixArgsPart"].Value.Split('\t');
                        var args = new List<string>();
                        var argMods = new List<string>();
                        knownRegexes.TryGetValue(ParseRegEx.RegexAffixFileArg, out parseRegex);
                        foreach (var argPart in linePart)
                        {
                            match = parseRegex.Match(argPart);

                            argMods.Add(match.Groups["argMod"].Success ? match.Groups["argMod"].Value : "");
                            args.Add(match.Groups["argName"].Value);
                        }
                        affixLineSource.ArgMods = argMods.ToArray();
                        affixLineSource.AffixLineArgs = args.ToArray();
                    }
                    result.Add(affixLineSource);
                }
                else
                {
                    Console.WriteLine($"[{fileName}.Read] Wrong affix line : {line}");
                    
                }
            }
            return result.ToArray();
        }
    }
}
