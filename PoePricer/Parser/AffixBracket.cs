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

    public enum AffixBracketType
    {
        AccuracyRating,
        Armour,
        LocalPhys,
        MaxMana,
        SpellDamage,
        StaffSpellDamage,
        StunRecovery,
        ComboLocalPhysAcc,
        ComboArmourStun,
        ComboSpellMana,
        ComboStaffSpellMana,
        AccuracyLightRadius
    }

    public struct ValueBracket
    {
        public int ItemLevel { get; set; }

        public int FirstAffixValueLo { get; set; }

        public int FirstAffixValueHi { get; set; }

        public int SecondAffixValueLo { get; set; }

        public int SecondAffixValueHi { get; set; }
    }

    public  class AffixBrackets : PricerDataReader
    {
        public string FirstAffix { get; set; }

        public string SecondAffix { get; set; }

        public ValueBracket[] Brackets { get; set; }



        public AffixBrackets(string fileName) : base(Path.Combine("AffixBrackets", fileName))
        {
            Brackets = Read(FileName);
        }

        private ValueBracket[] Read(string fileName)
        {
            var result = new List<ValueBracket>();

            var lines = this.RawLines; // RawLines impemented in base class which is PricerDataReader

            // regex could be tested here: https://regex101.com/r/dH6eO4/1

            var parseRegexAffixValues = fileName.Contains("Combo") ? new Regex(
                        @"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)\t+(?'secondValueLo'\d+)\-(?'secondValueHi'\d+)[ ]*$", RegexOptions.Compiled) : //ComboRegexp
                        fileName.Contains("LightRadius")
                    ? new Regex(@"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[%]*\t+(?'secondValueLo'\d+)[ ]*$", RegexOptions.Compiled) //LightRadiusRegexp
                    : new Regex(@"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[ ]*$", RegexOptions.Compiled); //DefaultRegexp


            var parseRegexAffixNames = (fileName.Contains("Combo") || fileName.Contains("LightRadius"))
                ? new Regex(@"^Aff\t+(?'firstAffixName'[A-zÀ-ÿ]+)\t+(?'secondAffixName'[A-zÀ-ÿ]+)[ ]*$", RegexOptions.Compiled)
                : new Regex(@"^Aff\t+(?'firstAffixName'[A-zÀ-ÿ]+)[ ]*$", RegexOptions.Compiled); //DefaultAffixNamesRegexp

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
                    SecondAffixValueLo = match.Groups["secondValueLo"].Success ? match.Groups["secondValueLo"].Value.ToInt() : 0,
                    SecondAffixValueHi = match.Groups["secondValueHi"].Success ? match.Groups["secondValueHi"].Value.ToInt() : 0,
                };
                result.Add(bracket);
            }
            //fileName.DumpToConsole();
            //parseRegexAffixNames.DumpToConsole();
            
            return result.ToArray();
        }

        public int GetAffixRangeFromiLevel(byte itemLevel, string affixName, string MinOrMax)
        {
            if ((MinOrMax != "Min") || (MinOrMax != "Max"))
            {
                Console.WriteLine($"[{this.FileName}.GetValueFromiLevel()] Wrong value for arg MinOrMax.");
                return 0;
            }
                
            if (affixName == FirstAffix)
            {
                for (var i = Brackets.Length - 1; i > 0; i--)
                {
                    if (Brackets[i].ItemLevel >= itemLevel) continue;
                    if (MinOrMax == "Min")
                        return Brackets[i].FirstAffixValueLo;
                    if (MinOrMax == "Max")
                        return Brackets[i].FirstAffixValueHi;
                }
            }
            if (affixName == SecondAffix)
            {
                for (var i = Brackets.Length - 1; i > 0; i--)
                {
                    if (Brackets[i].ItemLevel >= itemLevel) continue;
                    if (MinOrMax == "Min")
                        return Brackets[i].FirstAffixValueLo;
                    if (MinOrMax == "Max")
                        return Brackets[i].SecondAffixValueHi;
                }
            }
            Console.WriteLine($"[{this.FileName}.GetValueFromiLevel()] Wrong affixName: '{affixName}' .(example: GetValueFromiLevel(50, \"Armour_Hi\")");
            return 0;
        }

        public void GetAffixRange(string affixSourceName, int affixSourceValue, out int MinRangeValue, out int MaxRangeValue)
        {
            MinRangeValue = 0;
            MaxRangeValue = 0;

            if ((affixSourceName == FirstAffix) && (affixSourceName != ""))
            {
                foreach (var bracket in Brackets.Where(bracket => affixSourceValue <= bracket.FirstAffixValueHi))
                {
                    MinRangeValue = bracket.FirstAffixValueLo;
                    MaxRangeValue = bracket.FirstAffixValueHi;
                    return;
                }
                Console.WriteLine($"[{this.FileName}.GetValueRange()] {FirstAffix} wrong value: '{affixSourceValue}'");
                return;
            }
            if ((affixSourceName == SecondAffix) && (affixSourceName != ""))
            {
                foreach (var bracket in Brackets.Where(bracket => affixSourceValue < bracket.SecondAffixValueHi))
                {
                    MinRangeValue = bracket.SecondAffixValueLo;
                    MaxRangeValue = bracket.SecondAffixValueHi;
                    return;
                }
                Console.WriteLine($"[{this.FileName}t.GetValueRange()] {SecondAffix} wrong value: '{affixSourceValue}'");
                return;
            }
            Console.WriteLine($"[{this.FileName}.GetValueRange()] wrong affixName: '{affixSourceName}'");
        }

        public void GetAffixValueRangeFromAffixValue(string affixSourceName, int affixSourceValue, string affixTargetName, out int affixTargetMinValue, out int affixTargetMaxValue)
        {
            affixTargetMaxValue = 0;
            affixTargetMinValue = 0;
            Console.WriteLine(SecondAffix);
            if ((FirstAffix == null) || (SecondAffix == null))
            {
                Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong Method for that AffixBracket: {this.FileName}");
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
                        Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong affixTargetName '{affixTargetName}'");
                        return;
                    }
                    Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong {FirstAffix} Value '{affixSourceValue}'");
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
                        Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong affixTargetName '{affixTargetName}'");
                        return;
                    }
                    Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong {SecondAffix} Value '{affixSourceValue}'");
                    return;
                }
                Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] Wrong sourceAffixName '{affixSourceName}'");
                return;
            }
            Console.WriteLine($"[{this.FileName}.GetAffixRangeFromAffixValue()] null affixName:  affixSourceName =['{affixSourceValue}']  affixTargetName =['{affixTargetName}']");
         
        }
    }
}