using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PoePricer.Extensions;

namespace PoePricer.Parser
{
    public class FilterSource : PricerDataReader
    {
        public FilterSource(ItemClassType className) : base(Path.Combine("Filters"))
        {
            Filters = Read(className);
        }

        public FilterLine[] Filters { get; set; }
        public ItemClassType FilterClass { get; set; }


        public int Count => Filters.Length;

        private FilterLine[] Read(ItemClassType filterName)
        {
            var parseRegexLine = new Regex(@"^(?'filterArgs'([A-Za-z\+\-_!0-9,><=]+ {0,1})+) *$", RegexOptions.Compiled);
            var parseArgRegex =
                new Regex(
                    @"^(?'argType'(\+|-|-|__|_)){0,1}(?'argName'[A-Za-z]+)(?'argOperator'(>|>=|>=|<|=)){0,1}(?'argValue'[0-9,\.]+){0,1}$",
                    RegexOptions.Compiled);
            var sectionName = ItemClassType.Unknown;
            var result = new List<FilterLine>();
            var lines = RawLines;
            var filterArg = new FilterArg();


            foreach (var line in lines)
            {
                if ((line == "") || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    ItemClassType type;
                    if (!Enum.TryParse(line.Replace("[", "").Replace("]", ""), out type))
                    {
                        Console.WriteLine($"[{filterName}]Wrong section name: {line}");
                        continue;
                    }

                    sectionName = type;
                    continue;
                }

                if (sectionName != filterName)
                    continue;

                var match = parseRegexLine.Match(line.Replace(".", ","));
                if (!match.Success)
                {
                    Console.WriteLine($"[{filterName}]Wrong char(s) or excess space(s) at filter line: {line}");
                    continue;
                }


                var argsMatch = match.Groups["filterArgs"].Value.Split(' ');
                var filterArgs = new List<FilterArg>();
                var ttType = ToolTipTypes.Common;
                var skipLine = false;
                var tFilterTier = FilterTiers.high;


                foreach (var arg in argsMatch)
                {
                    var matcharg = parseArgRegex.Match(arg);


                    if (!matcharg.Success)
                    {
                        skipLine = true;
                        Console.WriteLine($"[{filterName}.FilterSource.Read] Wrong condition. {arg}");
                        continue;
                    }

                    var item = new Item();
                    var argOperator = ArgOperators.Empty;
                    var argType = ArgTypes.NonCraft;
                    double argValue = 0;
                    var argName = matcharg.Groups["argName"].Value;

                    if (matcharg.Groups["argType"].Success)
                    {
                        switch (matcharg.Groups["argType"].Value)
                        {
                            case "_":
                                if (!item.IsArgExist("Craft" + matcharg.Groups["argName"].Value))
                                {
                                    skipLine = true;
                                    Console.WriteLine(
                                        $"[{filterName}.FilterSource.Read] That arg not exist:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                    continue;
                                }
                                argType = ArgTypes.Craft;
                                argName = "Craft" + matcharg.Groups["argName"].Value;
                                break;
                            case "__":
                                if (!item.IsArgExist("Multi" + matcharg.Groups["argName"].Value))
                                {
                                    skipLine = true;
                                    Console.WriteLine(
                                        $"[{filterName}.FilterSource.Read] That arg not exist:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                    continue;
                                }
                                argType = ArgTypes.Craft;
                                argName = "Multi" + matcharg.Groups["argName"].Value;
                                break;
                            case "-":
                                if (!item.IsArgExist("Is" + matcharg.Groups["argName"].Value))
                                {
                                    skipLine = true;
                                    Console.WriteLine(
                                        $"[{filterName}.FilterSource.Read] That arg not exist:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                    continue;
                                }
                                argType = ArgTypes.NotExist;
                                argName = "Is" + matcharg.Groups["argName"].Value;
                                goto endofargparse;
                            case "+":
                                if (!item.IsArgExist("Is" + matcharg.Groups["argName"].Value))
                                {
                                    skipLine = true;
                                    Console.WriteLine(
                                        $"[{filterName}.FilterSource.Read] That arg not exist:  {matcharg.Groups["argType"].Value}{matcharg.Groups["argName"]} in filter line: {line}");
                                    continue;
                                }
                                argType = ArgTypes.Exist;
                                argName = "Is" + matcharg.Groups["argName"].Value;
                                goto endofargparse;
                        }
                    }

                    if (!item.IsArgExist(matcharg.Groups["argName"].Value))
                    {
                        if ((arg == "mid") || (arg == "low") || (arg == "high"))
                        {
                            Enum.TryParse(arg, out tFilterTier);
                            continue;
                        }

                        if ((arg == "COC") || (arg == "Phys") || (arg == "Elem") || (arg == "Spell") || (arg == "COC") ||
                            (arg == "LINKS"))
                        {
                            Enum.TryParse(arg, out ttType);

                            continue;
                        }
                        Console.WriteLine($"[{filterName}.FilterSource.Read] Wrong arg name: {arg} at line: {line}");
                        skipLine = true;
                        continue;
                    }


                    if (matcharg.Groups["argOperator"].Success)
                    {
                        switch (matcharg.Groups["argOperator"].Value)
                        {
                            case ">":
                                argOperator = ArgOperators.More;
                                break;
                            case "<":
                                argOperator = ArgOperators.Less;
                                break;
                            case ">=":
                                argOperator = ArgOperators.MoreEqual;
                                break;
                            case "<=":
                                argOperator = ArgOperators.LessEqual;
                                break;
                            default:
                                argOperator = ArgOperators.Equal;
                                break;
                        }
                    }


                    if (!double.TryParse(matcharg.Groups["argValue"].Value, out argValue))
                    {
                        Console.WriteLine(
                            $"[{filterName}.FilterSource.Read] Wrong value for Arg: {arg} line :{matcharg.Groups["argValue"].Value}");
                        skipLine = true;
                    }
                    endofargparse:
                    filterArg = new FilterArg
                    {
                        Name = argName,
                        Type = argType,
                        Operator = argOperator,
                        Value = argValue
                    };
                    filterArgs.Add(filterArg);
                }

                if (skipLine)
                {
                    continue;
                }

                FilterClass = filterName;
                var filter = new FilterLine
                {
                    Tier = tFilterTier,
                    Args = filterArgs,
                    ToolTipType = ttType,
                    RawLine = line
                };
                result.Add(filter);
            }

            return result.ToArray();
        } //read

        public void Scoring(Item item, FilterTiers tier = FilterTiers.low)
        {
            if (item.ClassType != FilterClass)
                return;

            var failArg = "";
            foreach (var filter in Filters)
            {
                if (filter.Tier > tier)
                {
                    ConsoleExtensions.WriteLine(
                        $"[{FilterClass}.Scoring] Inappropriate filter tier: {filter.RawLine}", ConsoleColor.Red);
                    continue;
                }

                foreach (var arg in filter.Args)
                {
                    switch (arg.Type)
                    {
                        case ArgTypes.Exist:
                            if (item.Get(arg.Name) == true)
                                continue;
                            break;
                        case ArgTypes.NotExist:
                            if (item.Get(arg.Name) == false)
                                continue;
                            break;
                        case ArgTypes.Craft:
                            if (ArgCompare(item.Get(arg.Name), arg.Operator, arg.Value))
                                continue;
                            break;
                        case ArgTypes.NonCraft:
                            if (ArgCompare(item.Get(arg.Name), arg.Operator, arg.Value))
                                continue;
                            break;
                    }

                    string oper;
                    switch (arg.Operator)
                    {
                        case ArgOperators.Equal:
                            oper = "=";
                            break;
                        case ArgOperators.LessEqual:
                            oper = "<=";
                            break;
                        case ArgOperators.MoreEqual:
                            oper = ">=";
                            break;
                        case ArgOperators.Less:
                            oper = "<";
                            break;
                        case ArgOperators.More:
                            oper = ">";
                            break;
                        default:
                            oper = "";
                            break;
                    }
                    failArg = arg.Name + " " + oper + " " + arg.Value;
                    goto skipfilter;
                }
                item.FilterSuccess = true;
                item.TtTypes.Remove(ToolTipTypes.Common);

                if (!item.TtTypes.Contains(filter.ToolTipType))
                    item.TtTypes.Add(filter.ToolTipType);
                ConsoleExtensions.WriteLine($"[{FilterClass}.FilterSource.Scoring] Success: {filter.RawLine} ",
                    ConsoleColor.Green);
                continue;
                skipfilter:
                ConsoleExtensions.WriteLine(
                    $"[{FilterClass}.Scoring] {filter.RawLine}\n {failArg}", ConsoleColor.Red);
            }
        }


        public bool ArgCompare(double fieldValue, ArgOperators argOperator, dynamic argValue)
        {
            switch (argOperator)
            {
                case ArgOperators.More:
                    if (fieldValue > argValue)
                        return true;
                    return false;
                case ArgOperators.MoreEqual:
                    if (fieldValue >= argValue)
                        return true;
                    return false;
                case ArgOperators.Less:
                    if (fieldValue < argValue)
                        return true;
                    return false;
                case ArgOperators.LessEqual:
                    if (fieldValue <= argValue)
                        return true;
                    return false;
                case ArgOperators.Equal:
                    if (Math.Abs(fieldValue - argValue) < 0.0001)
                        return true;
                    return false;
                default:
                    Console.WriteLine(
                        $"[{FilterClass}.FilterSource.ArgCompare(,,)] Wrong operator for that method. {argOperator}");
                    break;
            }
            return false;
        }
    }
}