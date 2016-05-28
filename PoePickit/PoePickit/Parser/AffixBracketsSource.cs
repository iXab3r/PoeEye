using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PoePricer.Extensions;

namespace PoePricer.Parser
{
    internal class AffixBracketsSource : PricerDataReader
    {
        public enum MinOrMax
        {
            Min,
            Max
        }

        public AffixBracketsSource(string fileName, IDictionary<ParseRegEx, Regex> knownRegexes)
            : base(Path.Combine("AffixBrackets", fileName))
        {
            Brackets = Read(FileName, knownRegexes);
        }

        public string FirstAffix { get; set; }

        public string SecondAffix { get; set; }

        public ValueBracket[] Brackets { get; set; }

        private ValueBracket[] Read(string fileName, IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            var result = new List<ValueBracket>();

            var lines = RawLines; // RawLines impemented in base class which is PricerDataReader

            // regex could be tested here: https://regex101.com/r/dH6eO4/1
            Regex parseRegex;
            if (fileName.Contains("Combo"))
            {
                knownRegexes.TryGetValue(ParseRegEx.RegexComboAffixBracketLine, out parseRegex);
            }
            else if (fileName.Contains("LightRadius"))
            {
                knownRegexes.TryGetValue(ParseRegEx.RegexLightAffixBracketLine, out parseRegex);
            }
            else
            {
                knownRegexes.TryGetValue(ParseRegEx.RegexAffixBracketLine, out parseRegex);
            }
            var parseRegexAffixValues = fileName.Contains("Combo")
                ? new Regex(
                    @"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)\t+(?'secondValueLo'\d+)\-(?'secondValueHi'\d+)[ ]*$")
                : //ComboRegexp
                fileName.Contains("LightRadius")
                    ? new Regex(
                        @"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[%]*\t+(?'secondValueLo'\d+)[ ]*$")
                    //LightRadiusRegexp
                    : new Regex(@"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[ ]*$"); //DefaultRegexp


            var parseRegexAffixNames = fileName.Contains("Combo") || fileName.Contains("LightRadius")
                ? new Regex(@"^Aff\t+(?'firstAffixName'[A-Za-z]+)\t+(?'secondAffixName'[A-Za-z�-�]+)[ ]*$")
                : new Regex(@"^Aff\t+(?'firstAffixName'[A-Za-z]+)[ ]*$"); //DefaultAffixNamesRegexp

            if (!parseRegexAffixNames.Match(lines[0]).Success)
            {
                Console.WriteLine($"[{fileName}.Read()] Affix Line missed at file.");
                return null;
            }

            FirstAffix = parseRegexAffixNames.Match(lines[0]).Groups["firstAffixName"].Success
                ? parseRegexAffixNames.Match(lines[0]).Groups["firstAffixName"].Value
                : "";

            SecondAffix = parseRegexAffixNames.Match(lines[0]).Groups["secondAffixName"].Success
                ? parseRegexAffixNames.Match(lines[0]).Groups["secondAffixName"].Value
                : "";


            foreach (var match in lines.Select(line => parseRegexAffixValues.Match(line)).Where(match => match.Success))
            {
                var bracket = new ValueBracket
                {
                    ItemLevel = match.Groups["itemLevel"].Value.ToInt(),
                    FirstAffixValueLo = match.Groups["valueLo"].Value.ToInt(),
                    FirstAffixValueHi = match.Groups["valueHi"].Value.ToInt(),
                    SecondAffixValueLo =
                        match.Groups["secondValueLo"].Success ? match.Groups["secondValueLo"].Value.ToInt() : 0,
                    SecondAffixValueHi =
                        match.Groups["secondValueHi"].Success ? match.Groups["secondValueHi"].Value.ToInt() : 0
                };
                result.Add(bracket);
            }

            return result.ToArray();
        }

        public int GetAffixMinMaxFromiLevel(int itemLevel, string affixName, MinOrMax minMax)
        {
            if ((minMax != MinOrMax.Max) && (minMax != MinOrMax.Min))
            {
                Console.WriteLine($"[{FileName}.GetAffixMinMaxFromiLevel()] Wrong value for arg MinOrMax.");
                return 0;
            }

            if (affixName == FirstAffix)
            {
                for (var i = Brackets.Length - 1; i > 0; i--)
                {
                    if (Brackets[i].ItemLevel >= itemLevel) continue;
                    if (minMax == MinOrMax.Max)
                        return Brackets[i].FirstAffixValueLo;
                    if (minMax == MinOrMax.Max)
                        return Brackets[i].FirstAffixValueHi;
                }
            }
            if (affixName == SecondAffix)
            {
                for (var i = Brackets.Length - 1; i > 0; i--)
                {
                    if (Brackets[i].ItemLevel >= itemLevel) continue;
                    switch (minMax)
                    {
                        case MinOrMax.Min:
                            return Brackets[i].FirstAffixValueLo;
                        case MinOrMax.Max:
                            return Brackets[i].SecondAffixValueHi;
                    }
                }
            }

            Console.WriteLine(
                $"[{FileName}.GetAffixMinMaxFromiLevel()] Wrong affixName: '{affixName}' ilevel: '{itemLevel}'. (example: GetValueFromiLevel(50, \"Armour_Hi\")");
            return 0;
        }

        public void GetAffixRange(string affixSourceName, int affixSourceValue, out int minRangeValue,
            out int maxRangeValue)
        {
            minRangeValue = 0;
            maxRangeValue = 0;

            if ((affixSourceName == FirstAffix) && (affixSourceName != ""))
            {
                foreach (var bracket in Brackets.Where(bracket => affixSourceValue <= bracket.FirstAffixValueHi))
                {
                    minRangeValue = bracket.FirstAffixValueLo;
                    maxRangeValue = bracket.FirstAffixValueHi;
                    return;
                }
                Console.WriteLine($"[{FileName}.GetValueRange()] {FirstAffix} wrong value: '{affixSourceValue}'");
                return;
            }
            if ((affixSourceName == SecondAffix) && (affixSourceName != ""))
            {
                foreach (var bracket in Brackets.Where(bracket => affixSourceValue < bracket.SecondAffixValueHi))
                {
                    minRangeValue = bracket.SecondAffixValueLo;
                    maxRangeValue = bracket.SecondAffixValueHi;
                    return;
                }
                Console.WriteLine($"[{FileName}t.GetValueRange()] {SecondAffix} wrong value: '{affixSourceValue}'");
                return;
            }
            Console.WriteLine($"[{FileName}.GetValueRange()] wrong affixName: '{affixSourceName}'");
        }

        public void GetAffixValueRangeFromAffixValue(string affixSourceName, int affixSourceValue,
            string affixTargetName, out int affixTargetMinValue, out int affixTargetMaxValue)
        {
            affixTargetMaxValue = 0;
            affixTargetMinValue = 0;
            Console.WriteLine(SecondAffix);
            if ((FirstAffix == null) || (SecondAffix == null))
            {
                Console.WriteLine(
                    $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong Method for that AffixBracket: {FileName}");
                return;
            }
            if ((affixSourceName != "") && (affixTargetName != ""))
            {
                if (affixSourceName == FirstAffix)
                {
                    foreach (var bracket in Brackets)
                    {
                        if ((affixSourceValue < bracket.FirstAffixValueLo) ||
                            (affixSourceValue > bracket.FirstAffixValueHi)) continue;
                        if (affixTargetName == FirstAffix)
                        {
                            affixTargetMinValue = bracket.FirstAffixValueLo;
                            affixTargetMaxValue = bracket.FirstAffixValueHi;
                            return;
                        }
                        if (affixTargetName == SecondAffix)
                        {
                            affixTargetMinValue = bracket.SecondAffixValueLo;
                            affixTargetMaxValue = bracket.SecondAffixValueHi;
                            return;
                        }
                        Console.WriteLine(
                            $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong affixTargetName '{affixTargetName}'");
                        return;
                    }
                    Console.WriteLine(
                        $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong {FirstAffix} Value '{affixSourceValue}'");
                    return;
                }
                if (affixSourceName == SecondAffix)
                {
                    foreach (var bracket in Brackets)
                    {
                        if ((affixSourceValue < bracket.SecondAffixValueLo) ||
                            (affixSourceValue > bracket.SecondAffixValueHi)) continue;
                        if (affixTargetName == FirstAffix)
                        {
                            affixTargetMinValue = bracket.FirstAffixValueLo;
                            affixTargetMaxValue = bracket.FirstAffixValueHi;
                            return;
                        }
                        if (affixTargetName == SecondAffix)
                        {
                            affixTargetMinValue = bracket.SecondAffixValueLo;
                            affixTargetMaxValue = bracket.SecondAffixValueHi;
                            return;
                        }
                        Console.WriteLine(
                            $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong affixTargetName '{affixTargetName}'");
                        return;
                    }
                    Console.WriteLine(
                        $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong {SecondAffix} Value '{affixSourceValue}'");
                    return;
                }
                Console.WriteLine(
                    $"[{FileName}.GetAffixRangeFromAffixValue()] Wrong sourceAffixName '{affixSourceName}'");
                return;
            }
            Console.WriteLine(
                $"[{FileName}.GetAffixRangeFromAffixValue()] null affixName:  affixSourceName =['{affixSourceValue}']  affixTargetName =['{affixTargetName}']");
        }
    }
}