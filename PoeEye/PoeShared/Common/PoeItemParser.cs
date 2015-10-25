namespace PoeShared.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Guards;

    using JetBrains.Annotations;

    using PoeTrade.Query;

    public sealed class PoeItemParser : IPoeItemParser
    {
        private const string BlocksSeparator = "--------";

        private readonly Regex rarityRegex = new Regex(@"^\s*Rarity\:\s*(.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex linksRegex = new Regex(@"^\s*Sockets\:\s*(.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex itemLevelRegex = new Regex(@"^\s*Item Level\:\s*(.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly PoeModInfo[] modsRegexes;

        public PoeItemParser([NotNull] IPoeQueryInfoProvider queryInfoProvider)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);

            modsRegexes = PrepareModsInfo(queryInfoProvider);
        }

        public IPoeItem Parse(string serializedItem)
        {
            Guard.ArgumentNotNull(() => serializedItem);

            var blockParsers = new List<Func<string, PoeItem, bool>>()
            {
                ParseItemRarityAndName,
                ParseItemCorruptionState,
                ParseRequirements,
                ParseItemLinks,
                ParseItemLevel,
                ParseItemImplicitMods,
                ParseItemExplicitMods,
            };

            var result = new PoeItem();

            var itemBlocks = SplitToBlocks(serializedItem);
            foreach (var block in itemBlocks)
            {
                var matchedParser = blockParsers.FirstOrDefault(x => x(block, result));

                if (matchedParser == null)
                {
                    continue;
                }

                foreach (var parser in blockParsers.ToArray())
                {
                    blockParsers.Remove(matchedParser);
                    if (parser == matchedParser)
                    {
                        break;
                    }
                }
            }

            TrimProperties(result);
            return result;
        }

        private bool ParseItemRarityAndName(string block, PoeItem item)
        {
            var splittedBlock = SplitToStrings(block);

            if (splittedBlock.Length < 2)
            {
                return false;
            }

            var rarityMatch = rarityRegex.Match(splittedBlock[0]);
            if (!rarityMatch.Success)
            {
                return false;
            }

            PoeItemRarity rarity;
            if (Enum.TryParse(rarityMatch.Groups[1].Value, out rarity))
            {
                item.Rarity = rarity;
            }

            item.ItemName = splittedBlock[1];

            return true;
        }

        private bool ParseItemCorruptionState(string block, PoeItem item)
        {
            if (block.IndexOf("Corrupted", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
            item.IsCorrupted = true;
            return true;
        }

        private bool ParseItemLinks(string block, PoeItem item)
        {
            var linksMatch = linksRegex.Match(block);
            if (!linksMatch.Success)
            {
                return false;
            }
            item.Links = new PoeLinksInfo(linksMatch.Groups[1].Value);
            return true;
        }

        private bool ParseRequirements(string block, PoeItem item)
        {
            var splittedBlock = SplitToStrings(block);

            if (splittedBlock.Length < 2)
            {
                return false;
            }

            if (splittedBlock[0].IndexOf("Requirements:", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            item.Requirements = string.Join(" ", splittedBlock.Skip(1));

            return true;
        }

        private bool ParseItemLevel(string block, PoeItem item)
        {
            var itemLevelMatch = itemLevelRegex.Match(block);
            if (!itemLevelMatch.Success)
            {
                return false;
            }
            item.Level = itemLevelMatch.Groups[1].Value;
            return true;
        }

        private bool ParseItemImplicitMods(string block, PoeItem item)
        {
            var splittedBlock = SplitToStrings(block);

            if (splittedBlock.Length != 1)
            {
                return false;
            }

            var mods = modsRegexes.Where(x => x.Mod.ModType == PoeModType.Implicit).ToArray();

            var possibleModString = splittedBlock[0];

            var mod = ParseItemMod(possibleModString, mods);
            if (mod != null)
            {
                item.Mods = item.Mods.Concat(new[] { mod }).ToArray();
                return true;
            }
            return false;
        }

        private bool ParseItemExplicitMods(string block, PoeItem item)
        {
            var splittedBlock = SplitToStrings(block);
            var mods = modsRegexes.Where(x => x.Mod.ModType == PoeModType.Explicit).ToArray();

            var parsedMods = new List<IPoeItemMod>();
            foreach (var possibleModString in splittedBlock)
            {
                var mod = ParseItemMod(possibleModString, mods);
                if (mod == null)
                {
                    continue;
                }
                parsedMods.Add(mod);
            }

            if (parsedMods.Any())
            {
                item.Mods = item.Mods.Concat(parsedMods).ToArray();
                return true;
            }
            return false;
        }

        private IPoeItemMod ParseItemMod(string possibleModString, PoeModInfo[] mods)
        {
            foreach (var poeModInfo in mods)
            {
                var match = poeModInfo.MatchingRegex.Match(possibleModString);
                if (!match.Success)
                {
                    continue;
                }

                var mod = new PoeItemMod()
                {
                    ModType = poeModInfo.Mod.ModType,
                    CodeName = poeModInfo.Mod.CodeName,
                    Name = possibleModString
                };

                return mod;
            }
            return null;
        }

        private static string[] SplitToBlocks(string serializedItem)
        {
            var rawBlocks = PrepareString(serializedItem)
               .Split(new[] { BlocksSeparator }, StringSplitOptions.RemoveEmptyEntries)
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .ToArray();
            return rawBlocks;
        }

        private static string[] SplitToStrings(string block)
        {
            return block
                .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(PrepareString)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }

        private static string PrepareString(string data)
        {
            return data.Trim(' ', '\t', '\n', '\r');
        }

        private static void TrimProperties<T>(T item)
        {
            var propertiesToProcess = typeof(T)
                .GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .Where(x => x.CanRead && x.CanWrite)
                .ToArray();

            foreach (var propertyInfo in propertiesToProcess)
            {
                var currentValue = (string)propertyInfo.GetValue(item);
                if (currentValue == null)
                {
                    continue;
                }

                var newValue = currentValue.Trim();
                propertyInfo.SetValue(item, newValue);
            }
        }

        private static PoeModInfo[] PrepareModsInfo(IPoeQueryInfoProvider provider)
        {
            var mods = provider.ModsList;
            return mods.Select(PrepareModInfo).ToArray();
        }

        private static PoeModInfo PrepareModInfo(IPoeItemMod mod)
        {
            const string digitPlaceholder = "DIGITPLACEHOLDER";
            var escapedRegexText = Regex.Escape(mod.CodeName.Replace("#", digitPlaceholder));
            var regexText = "^" + escapedRegexText.Replace(digitPlaceholder, "(.*?)") + "$";
            var regex = new Regex(regexText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return new PoeModInfo(mod, regex);
        }

        private struct PoeModInfo
        {
            public PoeModInfo(IPoeItemMod mod, Regex matchingRegex)
            {
                Mod = mod;
                MatchingRegex = matchingRegex;
            }

            public IPoeItemMod Mod { get; }

            public Regex MatchingRegex { get; }
        }
    }
}