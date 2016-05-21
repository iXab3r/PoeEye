using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using PoePickit.Extensions;

namespace PoePickit.Parser
{
    public enum FilterTypes
    {
        Dagger,
        OneHandedSword,
        OneHandedAxe,
        OneHandedMace,
        Sceptre,
        Claw,
        Wand,
        TwoHandedAxe,
        TwoHandedSword,
        TwoHandedMace,
        Staff,
        Bow,
        Boots,
        Gloves,
        Helm,
        BodyArmour,
        Ring,
        Amulet,
        Quiver,
        Belt,
        Shield
    }
   
    public enum FilterOperators
    {
        More,
        MoreEqual,
        Less,
        LessEqual,
        Equal,
        Exist,
        NonExist
    }

    public enum ToolTipTypes
    {
        Unknown,
        Common,
        PDPS,
        EDPS,
        COC,
        SPD,
    }
    public enum ArgTypes
    {
        NonCraft,
        Craftable,
        Craft,
        MultiCraft,
        NotExist,
        Exist
    }

    public struct FilterLine
    {
        public string[] ArgNames { get; set; }

        public FilterOperators[] ArgOperators { get; set; }

        public double[] ArgValues { get; set; }

        public string FilterTier { get; set; }

        public ArgTypes[] ArgType { get; set; }

        public ToolTipTypes ToolTipType { get; set; }

        public string RawLine { get; set; }
    }



    public class FilterSource : PricerDataReader
    {
        public FilterLine[] Filters { get; set; }
        public string FilterName { get; set; }

        public FilterSource(string filterName) : base(Path.Combine("Filter","Filters"))
        {
            Filters = Read(filterName);
        }

        private FilterLine[] Read(string filterName)
        {
            var result = new List<FilterLine>();
            var lines = RawLines;
            var parseRegexLine = new Regex(@"^(?'filterTier'mid|low|high){0,1} *(?'filterArgs'(((\+|\+\+|)[A-Za-z]+(>=|<=|<|>|=)[0-9\.,]+|(!|)[A-Za-z]) *)+) *(;|$)", RegexOptions.Compiled);
            var parseArgRegex = new Regex(@"^(?'argType'\+|\+\+|-|\!){0,1}(?'argName'[A-Za-z]+)(?'operator'(>|>=|>=|<|=)){0,1}(?'argValue'[TFtruefals0-9,]+){0,1}$", RegexOptions.Compiled);
            var sectionName = "";
            
            foreach (var line in lines)
            {
                if ((line == "") || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("["))
                {
                    var tLine = line.Replace("[", "").Replace("]", "");
                    if (sectionName == "")
                    {
                        if (tLine.Replace(" ","") != filterName)
                            continue;

                        FilterName = tLine;
                        sectionName = tLine;
                        continue;
                    }
                    {
                        if (tLine.Replace(" ", "") != filterName)
                            sectionName = "";
                        continue;
                    }
                }

                if (sectionName == "")
                    continue;
               

                var match = parseRegexLine.Match(line.Replace(".", ","));
                if (!match.Success) 
                {
                    Console.WriteLine($"[{filterName}]WrongFilterLine: {line}");
                    continue;
                }

                var args = match.Groups["filterArgs"].Value.Split(' ');
                var argTypes = new List<ArgTypes>();
                var argNames = new List<string>();
                var argValues = new List<double>();
                var argOperators = new List<FilterOperators>();
                var ttType = ToolTipTypes.Unknown;
                var skipLine = false;
                var tFilterTier = match.Groups["filterTier"].Success ? match.Groups["filterTier"].Value : "high";

                foreach (var arg in args)
                {
                    var matcharg = parseArgRegex.Match(arg);
                    if (arg == "")
                    {
                        skipLine = true;
                        continue;
                    }
                    
                    var item = new Item();
                    switch (matcharg.Groups["argType"].Value)
                    {
                        case "+":
                            if (item.Get("Craft" + matcharg.Groups["argName"].Value) == -1)
                            {
                                skipLine = true;
                                Console.WriteLine(
                                    $"[{filterName}.FilterSource.Read] Wrong arg:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                Console.ReadKey();
                                continue;
                            }
                            argTypes.Add(ArgTypes.Craft);
                            break;
                        case "++":
                            if (item.Get("Multi" + matcharg.Groups["argName"].Value) == -1)
                            {
                                skipLine = true;
                                Console.WriteLine(
                                    $"[{filterName}.FilterSource.Read] Wrong arg:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                Console.ReadKey();
                                continue;
                            }
                            argTypes.Add(ArgTypes.MultiCraft);
                            break;
                        case "!":
                            argTypes.Add(ArgTypes.NotExist);
                            break;
                        default:
                            argTypes.Add(ArgTypes.Exist);
                            break;
                    }

                    switch (matcharg.Groups["argName"].Value)
                    {
                        case "PDPS":
                        case "PAPS":
                        case "PCrit":
                        case "PCritDamage":
                            ttType = ToolTipTypes.PDPS;
                            break;
                        case "EDPS":
                        case "EAPS":
                        case "ECrit":
                        case "ECritDamage":
                        ttType = ToolTipTypes.EDPS;
                            break;
                        case "COCSPD":
                        case "COCTotalSPD":
                        case "COCCrit":
                        case "COCCritDamage":
                        case "COCSpellCrit":
                        case "COCCAPS":
                            ttType = ToolTipTypes.COC;
                            break;
                        case "SPD":
                        case "TotalSPD":
                        case "FlatSPD":
                        case "CastSpeed":
                            if (!item.IsWeapon)
                                ttType = ToolTipTypes.SPD;
                            else
                                ttType = ToolTipTypes.Common;
                            break;
                        default:
                            if (ttType == ToolTipTypes.Unknown)
                                ttType = ToolTipTypes.Common;
                            break;
                    }
                    
                    if (!matcharg.Groups["operator"].Success)
                    {

                        if (item.Get("Is" + matcharg.Groups["argName"].Value) != false)
                        {
                            skipLine = true;
                            Console.WriteLine(
                                $"[{filterName}.FilterSource.Read] Wrong arg:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                            Console.ReadKey();
                        }
                        argOperators.Add(matcharg.Groups["argType"].Value == "!"
                            ? FilterOperators.NonExist
                            : FilterOperators.Exist);
                        argNames.Add("Is" + matcharg.Groups["argName"].Value);
                        argValues.Add(-1);

                    }
                    else
                    {
                        argNames.Add(matcharg.Groups["argName"].Value);
                        switch (matcharg.Groups["operator"].Value)
                        {
                            case ">":
                                argOperators.Add(FilterOperators.More);
                                break;
                            case "<=":
                                argOperators.Add(FilterOperators.LessEqual);
                                break;
                            case "<":
                                argOperators.Add(FilterOperators.Less);
                                break;
                            case ">=":
                                argOperators.Add(FilterOperators.MoreEqual);
                                break;
                            case "=":
                                argOperators.Add(FilterOperators.Equal);
                                break;
                            default:
                                Console.WriteLine(
                                    $"[{filterName}.FilterSource.Read] Wrong operator {matcharg.Groups["operator"].Value} in line: {line}");
                                skipLine = true;
                                break;
                        }
                        double argValue;

                        if (double.TryParse(matcharg.Groups["argValue"].Value, out argValue))
                        {
                            argValues.Add(argValue);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"[{filterName}.FilterSource.Read] Wrong value for Arg: {arg} line :{matcharg.Groups["argValue"].Value}");
                            skipLine = true;
                        }

                    }

                }

                if (skipLine)
                {
                    Console.WriteLine($"[{filterName}.FilterSource.Read] Error(s) in line: {line}");
                    continue;
                }

                var filter = new FilterLine
                {
                    FilterTier = tFilterTier,
                    ArgNames = argNames.ToArray(),
                    ArgType = argTypes.ToArray(),
                    ArgOperators = argOperators.ToArray(),
                    ArgValues = argValues.ToArray(),
                    ToolTipType = ttType,
                    RawLine = line
                };
                result.Add(filter);
            }
            
            return result.ToArray();

        }//read


        public int Count => Filters.Length;

        public void Scoring(Item item)
        {
            
            if (item.ClassType != FilterName)
               return;
            foreach (var filter in Filters)
            {
                for (var i = 0; i < filter.ArgNames.Length; i++)
                {
                    string tName;
                    
                    switch (filter.ArgType[i])
                    {
                        case ArgTypes.Craft:
                            tName = "Craft" + filter.ArgNames[i];
                            break;
                        case ArgTypes.MultiCraft:
                            tName = "Multi" + filter.ArgNames[i];
                            break;
                        default:
                            tName = filter.ArgNames[i];
                            break;
                    }
                    
                    if (item.GetType().GetProperty(tName) != null)
                    {
                        Console.WriteLine(
                            $"[{FilterName}.FilterSource.Scoring] Wrong arg or operator for that arg {tName} \n In FilterLine: {filter.RawLine}");
                        continue;
                    }

                    if ((filter.ArgOperators[i] == FilterOperators.Exist) || (filter.ArgOperators[i] == FilterOperators.NonExist))
                    {
                        if (!ArgCompare(item.Get(tName), filter.ArgOperators[i]))
                        {
                            ConsoleExtensions.WriteLine(
                                $"[{FilterName}.FilterSource.Scoring] Fail    : {tName} in {filter.RawLine}",ConsoleColor.Red);
                            break;
                        }
                    }
                    else
                    {
                        if (
                            !ArgCompare(Convert.ToDouble(item.Get(tName)), filter.ArgOperators[i],
                                filter.ArgValues[i]))
                        {
                            ConsoleExtensions.WriteLine(
                                $"[{FilterName}.FilterSource.Scoring] Fail    : {tName}={Convert.ToDouble(item.Get(tName))} in {filter.RawLine}",ConsoleColor.Red);
                            break;
                        }
                    }

                    if (i == filter.ArgNames.Length - 1)
                    {
                        ConsoleExtensions.WriteLine($"[{FilterName}.FilterSource.Scoring] Success : {filter.RawLine} ", ConsoleColor.Green);
                        item.FilterSuccess = true;
                        item.TtTypes.Remove(ToolTipTypes.Common);
                        if (!item.TtTypes.Contains(filter.ToolTipType))
                            item.TtTypes.Add(filter.ToolTipType);
                        
                    }
                }
                
                
            }
        }

        

        public void CreateTempToolTip(Item item, ToolTipTypes ttType)
        {
            var tTt = "\n------------------- " + item.ClassType + " -------------------";
            switch (ttType)
            {
                case ToolTipTypes.PDPS:
                    tTt = tTt + "\nPhysDPS:".PadRight(20) + item.PDPS.ToString().PadLeft(20) + "\nAPS:".PadRight(20) + item.APS.ToString().PadLeft(20) + "\nCrit:".PadRight(20) + item.Crit.ToString().PadLeft(20) + "\n------------------------";
                    if (item.MultiPDPS > item.PDPS)
                    {
                        tTt = tTt + "\nPhysDPS:".PadRight(20) + (item.MultiPDPSLo + "-" + item.MultiPDPS).PadLeft(20) + "    " + item.MultiTtPDPS +
                              "\nAPS:".PadRight(20) + (item.MultiPAPSLo + "-" + item.MultiPAPS).PadLeft(20) + "\nCrit:".PadRight(20) + item.MultiPCrit.ToString().PadLeft(20) + "\n------------------------";
                    }
                    break;
                case ToolTipTypes.EDPS:
                    tTt = tTt + "\nElemDPS:".PadRight(20) + item.EDPS.ToString().PadLeft(20) + "\nAPS:".PadRight(20) + item.APS.ToString().PadLeft(20) + "\nCrit:".PadRight(20) + item.Crit.ToString().PadLeft(20) + "\n------------------------";
                    if (item.MultiEDPS > item.EDPS)
                    {
                        tTt = tTt + "\nElemDPS:".PadRight(20) + (item.MultiEDPSLo + "-" + item.MultiEDPS).PadLeft(20) + "    " + item.MultiTtEDPS +
                              "\nAPS:".PadRight(20) + (item.MultiEAPSLo + "-" + item.MultiEAPS).PadLeft(20) + "\nCrit:".PadRight(20) + item.Crit.ToString().PadLeft(20) + "\n------------------------";
                    }

                    break;
                default:
                    
                    break;
            }
            item.Tt = item.Tt + "\n" + tTt;
        }

        public bool ArgCompare(bool fieldValue, FilterOperators filterOperator)
        {
            switch (filterOperator)
            {
                case FilterOperators.Exist:
                    if (fieldValue == true)
                        return true;
                    return false;
                case FilterOperators.NonExist:
                    if (fieldValue == true)
                        return false;
                    return true;
                default:
                    Console.WriteLine($"[{FilterName}.FilterSource.ArgCompare(,)] Wrong operator for that method. {filterOperator}");
                    break;
            }
            return false;
        }

        public bool ArgCompare(double fieldValue, FilterOperators filterOperator, dynamic argValue)
        {
            switch (filterOperator)
            {
                case FilterOperators.More:
                    if (fieldValue > argValue)
                        return true;
                    return false;
                case FilterOperators.MoreEqual:
                    if (fieldValue >= argValue)
                        return true;
                    return false;
                case FilterOperators.Less:
                    if (fieldValue < argValue)
                        return true;
                    return false;
                case FilterOperators.LessEqual:
                    if (fieldValue <= argValue)
                        return true;
                    return false;
                case FilterOperators.Equal:
                    double tValue;
                    if (double.TryParse(argValue, out tValue))
                    {
                        if (Math.Abs(fieldValue - argValue) < 0.0001)
                            return true;
                    }
                    return false;
                default:
                    Console.WriteLine($"[{FilterName}.FilterSource.ArgCompare(,,)] Wrong operator for that method. {filterOperator}");
                    break;
            }
            return false;
        }
    }
}