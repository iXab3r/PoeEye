namespace PoeEyeUi.PoeTrade.Models
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Config;

    using DumpToText;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;

    internal sealed class PoePriceCalculcator : IPoePriceCalculcator
    {
        private readonly IDictionary<string, float> currencyByType;

        private readonly Regex currencyParser = new Regex(@"(?'value'[\d\.\,]*)\s*(?'type'\w*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoePriceCalculcator([NotNull] IPoeEyeConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            currencyByType = config.CurrenciesPriceInChaos.ToDictionary(x => x.Key, x => x.Value);
            Log.Instance.Debug($"[PriceCalculcator] Currencies list:\r\n{currencyByType.DumpToTextValue()}");
        }

        public float? GetEquivalentInChaosOrbs(string rawPrice)
        {
            if (rawPrice == null)
            {
                return null;
            }

            var match = currencyParser.Match(rawPrice);
            if (!match.Success)
            {
                return null;
            }

            var currencyValueString = match.Groups["value"].Value;
            var currencyTypeString = match.Groups["type"].Value;
            float currencyValue;

            if (!float.TryParse(currencyValueString, NumberStyles.Any, CultureInfo.InvariantCulture, out currencyValue))
            {
                Log.Instance.Debug(
                    $"[PriceCalculcator] Could not convert value '{currencyValueString}' to float, rawPrice: {rawPrice}");
                return null;
            }

            float currencyMultilplier;
            if (!currencyByType.TryGetValue(currencyTypeString, out currencyMultilplier))
            {
                Log.Instance.Debug(
                    $"[PriceCalculcator] Could not convert currency type '{currencyTypeString}' to multiplier, rawPrice: {rawPrice}\r\nMultipliers:{currencyByType.DumpToTextValue()}");
                return null;
            }

            return currencyValue*currencyMultilplier;
        }
    }
}