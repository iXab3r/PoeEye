using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using JetBrains.Annotations;
using Microsoft.JScript;
using PoePickit.Extensions;
using PoePickit.Parser;

namespace PoePickit.Parser
{
    public struct UniqueItem
    {
        public List<UniqueAffix> Affixes;
        public string WebLink;
        public string Desc;
    }

    public struct UniqueAffix
    {
        public string[] WordsInLine;
        public string RawLine;
        public double LoValue;
        public double HighValue;
        public double LowValueSecond;
        public double HighValueSecond;
        public bool IsImplicit;
        public bool IsDoubleAffix;
    }

    public class UniqueAffixSource : PricerDataReader
    {

        public Dictionary<ItemClassType, Dictionary<string, UniqueItem>> KnownUniques;
        public UniqueItem UnknownUnique = new UniqueItem() {Desc = "Данный предмет отсутсвутет в базе легендарок."};

        public UniqueAffixSource(string fileName = "UniqueAffixes") : base(Path.Combine(fileName))
        {
            KnownUniques = Read(fileName);
        }
        
        public Dictionary<ItemClassType, Dictionary<string, UniqueItem>> Read(string fileName)
        {

        var parseRegexUniqueAffixLine =
                new Regex(
                    @"^(?'uniqueName'[A-Za-zö', ]+)\|(?'affixLines'([@A-Za-z-0-9:\|\-\.,%\(\)<>' ])+) *(;(?'webLink'.*)){0,1}$",
                    RegexOptions.Compiled);

            var parseRegexUniqueAffixRange =
                new Regex(@"^(?'rangeAffixLo'[@0-9\(\)\-\.]+)-(?'rangeAffixHi'[0-9\(\)\-\.]+):(?'affix'[A-Za-z\.'%<> ]+)$",
                    RegexOptions.Compiled);

            var parseRegexUniqueAffix =
                new Regex(@"^((?'valueAffix'(-){0,1}(\(){0,1}[@0-9\.A-Za-z ]+(\)){0,1}):){0,1}(?'affix'[A-Za-z\-\.0-9',%<> ]+)$", RegexOptions.Compiled);

            var parseRegexUniqueAffixRangeDouble =
                new Regex(
                    @"^(?'rangeAffixLo'[0-9\-\.]+)-(?'rangeAffixHi'[0-9\-\.]+),(?'rangeAffixSecondLo'[0-9\-\.]+)-(?'rangeAffixSecondHi'[0-9\-\.]+):(?'affix'[A-Za-z\.'%<> ]+)$",
                    RegexOptions.Compiled);




            var knownUniques = new Dictionary<ItemClassType, Dictionary<string, UniqueItem>>();
            var type = ItemClassType.Unknown;
            var uniqueItems = new Dictionary<string, UniqueItem>();

            foreach (var line in RawLines)
            {
                
                if ((line == "") || (line == " ") || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var lastType = type;
                    
                    if (!Enum.TryParse(line.Replace("[","").Replace("]",""), out type))
                    {
                        
                        Console.WriteLine($"[{fileName}]Wrong section name: {line}");
                        type = ItemClassType.Unknown;
                        continue;
                    }
                    
                    if (lastType != ItemClassType.Unknown)
                    {
                        knownUniques.Add(lastType, new Dictionary<string, UniqueItem>(uniqueItems));
                    }
                    
                    uniqueItems.Clear();
                    continue;
                }
                if (type == ItemClassType.Unknown)
                    continue;
                var matchline = parseRegexUniqueAffixLine.Match(line);
                if (!matchline.Success)
                {
                    Console.WriteLine($"[{fileName}]Wrong line at file with uniques: {line}");
                    continue;
                }


                var affixeLines = matchline.Groups["affixLines"].Value.Split('|');
              
                var affixes = new List<UniqueAffix>();
                
                foreach (var affix in affixeLines)
                {
                    if (parseRegexUniqueAffix.Match(affix).Success)
                        continue;
                    var isDoubleAffix = false;
                    Match match;
                    if (affix.Contains(","))
                    {
                        match = parseRegexUniqueAffixRangeDouble.Match(affix);
                        isDoubleAffix = true;
                    }
                    else
                        match = parseRegexUniqueAffixRange.Match(affix);

                    if (!match.Success)
                    {
                        Console.WriteLine($"[{fileName}]Wrong affix: {affix}\n at unique file line: {line}\n section: {type}");
                        Console.ReadKey();
                        continue;
                    }
                    
                     
                    var valueLoString = match.Groups["rangeAffixLo"].Value.Replace(".",",");
                    var valueHiString = match.Groups["rangeAffixHi"].Value.Replace(".", ",");
                    var valueLoStringSecond = match.Groups["rangeAffixSecondLo"].Value.Replace(".", ",");
                    var valueHiStringSecond = match.Groups["rangeAffixSecondHi"].Value.Replace(".", ",");
                    var valueLo = 0d;
                    var valueHi = 0d;
                    var valueLoSecond = 0d;
                    var valueHiSecond = 0d;
                    var isImplicit = false;
                    var skipaffix = false;
                    if (match.Groups["rangeAffixLo"].Value.StartsWith("@"))
                    {
                        isImplicit = true;
                        valueLoString = valueLoString.Replace("@", "");
                    }
                    
                    if (!double.TryParse(valueLoString, out valueLo))
                    {
                        Console.WriteLine($"[{fileName}]Wrong affix range vL: {valueLoString} at uniqie file line: {line} section: {type}");
                        Console.ReadKey();
                        skipaffix = true;
                    }
                    if (!double.TryParse(valueHiString, out valueHi))
                    {
                        Console.WriteLine($"[{fileName}]Wrong affix range vH: {valueHiString} at uniqie file line: {line} section: {type}");
                        Console.ReadKey();
                        skipaffix = true;
                    }
                    
                    if (valueLoStringSecond != "")
                    if (!double.TryParse(valueLoStringSecond, out valueLoSecond))
                    {
                        Console.WriteLine($"[{fileName}]Wrong affix range vLS: {valueLoStringSecond} at uniqie file line: {line} section: {type}");
                        Console.ReadKey();
                        skipaffix = true;
                    }
                    if (valueHiStringSecond != "")
                        if (!double.TryParse(valueHiStringSecond, out valueHiSecond))
                    {
                        Console.WriteLine($"[{fileName}]Wrong affix range vHS: {valueHiStringSecond} at uniqie file line: {line} section: {type}");
                        Console.ReadKey();
                        skipaffix = true;
                    }
                    if (skipaffix)
                        continue;

                    var uniqueAffixe = new UniqueAffix
                    {
                        WordsInLine = match.Groups["affix"].Value.Split(' '),
                        RawLine = match.Groups["affix"].Value,
                        LoValue = valueLo,
                        HighValue = valueHi,
                        LowValueSecond = valueLoSecond,
                        HighValueSecond = valueHiSecond,
                        IsImplicit = isImplicit,
                        IsDoubleAffix = isDoubleAffix
                    };
                    affixes.Add(uniqueAffixe);
                }
                var tip = "";
                if (matchline.Groups["uniqueName"].Value == "Ventor's Gamble")
                    tip = "Если у вас проблемы с \nдоступом к JOYCASINO.COM,\n то просто киньте Divine Orb \n в это кольцо.";
                
                
                var uniqueItem = new UniqueItem
                {
                    Affixes = affixes,
                    WebLink = matchline.Groups["webLink"].Value,
                    Desc = tip
                };
                uniqueItems.Add(matchline.Groups["uniqueName"].Value, uniqueItem);
                
            }
            return knownUniques;
        }

        public UniqueItem GetUnique(string name, ItemClassType type)
        {
            var uniques = new Dictionary<string, UniqueItem>();
            if (!KnownUniques.TryGetValue(type, out uniques))
            {
                Console.WriteLine($"[{FileName}.GetUnique] Wrong item type {type}. ");
                return UnknownUnique;
            }
            var unique = new UniqueItem();
            if (!uniques.TryGetValue(name, out unique))
            {
                Console.WriteLine($"[{FileName}.GetUnique] Wrong item name {name}. ");
                return UnknownUnique;
            }
            return unique;
        }


      
    }
}
