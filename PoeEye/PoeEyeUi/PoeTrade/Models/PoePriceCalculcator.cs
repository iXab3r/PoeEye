namespace PoeEyeUi.PoeTrade.Models
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Config;

    using Converters;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.DumpToText;

    internal sealed class PoePriceCalculcator : IPoePriceCalculcator
    {
        private readonly IDictionary<string, float> currencyByType;


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

            var price = PriceToCurrencyConverter.Instance.Convert(rawPrice);
            if (price == null)
            {
                return null;
            }

            float currencyMultilplier;
            if (!currencyByType.TryGetValue(price.CurrencyType, out currencyMultilplier))
            {
                Log.Instance.Debug(
                    $"[PriceCalculcator] Could not convert currency type '{price.CurrencyType}' to multiplier, rawPrice: {rawPrice}\r\nMultipliers:{currencyByType.DumpToTextValue()}");
                return null;
            }

            return price.Value * currencyMultilplier;
        }
    }
}