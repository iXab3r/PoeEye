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

        private readonly IPoeQueryInfoProvider queryInfoProvider;

        public PoeItemParser([NotNull] IPoeQueryInfoProvider queryInfoProvider)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);
            
            this.queryInfoProvider = queryInfoProvider;
        }

        public IPoeItem Parse([NotNull] string serializedItem)
        {
            Guard.ArgumentNotNull(() => serializedItem);

            var blockParsers = new List<Func<string, PoeItem, bool>>()
            {
                ParseItemRarityAndName,
                ParseItemCorruptionState,
                ParseItemLinks
            };

            var result = new PoeItem();

            var itemBlocks = SplitToBlocks(serializedItem);
            foreach (var block in itemBlocks)
            {
                var parsersToRemove = blockParsers
                    .Select(x => new { Parser = x, IsMatch = x(block, result) })
                    .Where(x => x.IsMatch)
                    .Select(x => x.Parser)
                    .ToArray();

                foreach (var parser in parsersToRemove)
                {
                    blockParsers.Remove(parser);
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
    }
}