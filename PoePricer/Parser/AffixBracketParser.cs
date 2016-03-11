namespace PoePricer.Parser
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Extensions;

    internal sealed class AffixBracketsSource : PricerDataReader
    {
        public AffixBracketsSource(string fileName) : base(Path.Combine("AffixBrackets", fileName))
        {
            Brackets = Read();
        }

        public AffixBracket[] Brackets { get; }

        private AffixBracket[] Read()
        {
            var lines = this.RawLines; // RawLines impemented in base class which is PricerDataReader

            // regex could be tested here: https://regex101.com/r/dH6eO4/1
            var parseRegex = new Regex(@"^(?'itemLevel'\d+)\t*(?'valueLo'\d+)\-(?'valueHi'\d+).*$", RegexOptions.Compiled);

            var result = new List<AffixBracket>();

            /*
            Also could be written like this:
            
            foreach (var line in lines)
            {
                var match = parseRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                var bracket = new AffixBracket
                {
                    ItemLevel = match.Groups["itemLevel"].Value.ToInt(),
                    ValueLo = match.Groups["valueLo"].Value.ToInt(),
                    ValueHi = match.Groups["valueHi"].Value.ToInt(),
                };

                result.Add(bracket);
            }          
           
            */

            foreach (var match in lines.Select(line => parseRegex.Match(line)).Where(match => match.Success))
            {
                var bracket = new AffixBracket
                {
                    ItemLevel = match.Groups["itemLevel"].Value.ToInt(),
                    ValueLo = match.Groups["valueLo"].Value.ToInt(),
                    ValueHi = match.Groups["valueHi"].Value.ToInt(),
                };

                result.Add(bracket);
            }

            return result.ToArray();
        }
    }
}