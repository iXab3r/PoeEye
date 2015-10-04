namespace PoeEyeUi.PoeTrade.Models
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using DumpToText;

    using PoeShared;

    internal sealed class PoePriceCalculcator : IPoePriceCalculcator
    {
        private readonly IDictionary<string, float> currencyByType = new Dictionary<string, float>
        {
            {"blessed", 2},
            {"chisel", 1},
            {"chaos", 1},
            {"chromatic", 0.5f},
            {"divine", 1},
            {"exalted", 60},
            {"gcp", 2},
            {"jewellers", 0.14f},
            {"alchemy", 0.5f},
            {"alteration", 0.05f},
            {"chance", 0},
            {"fusing", 0.5f},
            {"regret", 2},
            {"scouring", 1},
            {"regal", 1}
        };

        private readonly Regex currencyParser = new Regex(@"(?'value'[\d\.\,]*)\s*(?'type'\w*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoePriceCalculcator()
        {
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