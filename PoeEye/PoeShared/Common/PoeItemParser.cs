namespace PoeShared.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Guards;

    using JetBrains.Annotations;

    using PoeTrade.Query;

    public sealed class PoeItemParser : IPoeItemParser
    {
        private const string BlocksSeparator = "--------";

        private readonly IPoeQueryInfoProvider queryInfoProvider;

        public PoeItemParser([NotNull] IPoeQueryInfoProvider queryInfoProvider)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);
            
            this.queryInfoProvider = queryInfoProvider;
        }

        public IPoeItem Parse([NotNull] string serializedItem)
        {
            Guard.ArgumentNotNull(() => serializedItem);
            
            var result = new PoeItem();

            var itemBlocks = PrepareString(serializedItem)
                .Split(new [] { BlocksSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            var blockParsers = new List<Func<string, PoeItem, bool>>()
            {
                ParseItemRarityAndName,
                ParseItemCorruptionState
            };

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
            if (block.IndexOf("Rarity", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
            var splittedBlock = SplitToStrings(block);

            if (splittedBlock.Length < 2)
            {
                return false;
            }

            item.ItemName = splittedBlock[1];

            return true;
        }

        private bool ParseItemCorruptionState(string block, PoeItem item)
        {
            if (block.IndexOf("Corrupted", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            return true;
        }

        private string[] SplitToStrings(string block)
        {
            return block
                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Select(PrepareString)
                .ToArray();
        }

        private static string PrepareString(string data)
        {
            return data.Trim(' ', '\t');
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