using System;
using System.Linq;
using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;
using PoeEye.ItemParser.Services;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.ItemParser.Converters
{
    internal sealed class PoeItemToPoeQueryConverter : IConverter<IPoeItem, IPoeQueryInfo>
    {
        private readonly PoeModParser[] modsRegexes;

        private readonly float valueRangeModifier = 0.5f;

        public PoeItemToPoeQueryConverter([NotNull] IPoeModsProcessor modsProcessor)
        {
            Guard.ArgumentNotNull(modsProcessor, nameof(modsProcessor));

            modsRegexes = modsProcessor.GetKnownParsers();
        }

        public IPoeQueryInfo Convert(IPoeItem value)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            var query = new PoeQueryInfo
            {
                ItemName = value.ItemName,
                OnlineOnly = true,
                NormalizeQuality = true,
                IsExpanded = true
            };

            var mods = value.Mods.ToArray();
            query.Mods = mods.Select(Convert).OfType<IPoeQueryRangeModArgument>().ToArray();

            return query;
        }

        private PoeQueryRangeModArgument Convert(IPoeItemMod mod)
        {
            var result = new PoeQueryRangeModArgument(mod);

            foreach (var poeModInfo in modsRegexes)
            {
                var match = poeModInfo.MatchingRegex.Match(mod.Name);

                if (!match.Success || match.Groups.Count < 2)
                {
                    continue;
                }

                var values = match
                    .Groups
                    .OfType<Group>()
                    .Skip(1)
                    .Select(x => x.Value)
                    .ToArray();

                if (values.Length > 1)
                {
                    //TODO Support multi-value mods
                    // e.g Adds #-# Chaos Damage
                    continue;
                }

                var valueToParse = values.Single();

                float parsedValue;
                if (!float.TryParse(valueToParse, out parsedValue))
                {
                    continue;
                }

                var minRange = parsedValue - valueRangeModifier * parsedValue;
                var maxRange = parsedValue + valueRangeModifier * parsedValue;

                result.Min = (float) Math.Round(minRange, 1);
                result.Max = (float) Math.Round(maxRange, 1);
            }

            return result;
        }
    }
}
