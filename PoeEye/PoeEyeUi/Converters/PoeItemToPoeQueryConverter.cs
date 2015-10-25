namespace PoeEyeUi.Converters
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using TypeConverter;

    internal sealed class PoeItemToPoeQueryConverter : IConverter<IPoeItem, IPoeQueryInfo>
    {
        private readonly PoeModInfo[] modsRegexes;

        private readonly float valueRangeModifier = 0.5f;

        public PoeItemToPoeQueryConverter([NotNull] IPoeQueryInfoProvider queryInfoProvider)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);
            
            modsRegexes = PrepareModsInfo(queryInfoProvider);
        }

        public IPoeQueryInfo Convert(IPoeItem value)
        {
            Guard.ArgumentNotNull(() => value);

            var query = new PoeQueryInfo()
            {
                ItemName = value.ItemName,

                BuyoutOnly = true,
                OnlineOnly = true,
                NormalizeQuality = true,
                IsExpanded = true
            };

            var implicitMod = value.Mods.SingleOrDefault(x => x.ModType == PoeModType.Implicit);
            if (implicitMod != null)
            {
                query.ImplicitMod = Convert(implicitMod);
            }

            var explicitMods = value.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            query.ExplicitMods = explicitMods.Select(Convert).OfType<IPoeQueryRangeModArgument>().ToArray();

            return query;
        }

        private PoeQueryRangeModArgument Convert(IPoeItemMod mod)
        {
            var result = new PoeQueryRangeModArgument(mod.CodeName);

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

                var minRange = parsedValue - valueRangeModifier*parsedValue;
                var maxRange = parsedValue + valueRangeModifier*parsedValue;

                result.Min = (float)Math.Round(minRange, 1);
                result.Max = (float)Math.Round(maxRange, 1);
            }

            return result;
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