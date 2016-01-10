namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using Config;

    using Converters;

    using CsQuery.ExtensionMethods.Internal;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.DumpToText;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class PoePriceCalculcator : DisposableReactiveObject, IPoePriceCalculcator
    {
        private readonly IDictionary<string, float> currencyByType = new Dictionary<string, float>();

        public PoePriceCalculcator([NotNull] IPoeEyeConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(() => configProvider);

            configProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Select(x => x.CurrenciesPriceInChaos)
                .Select(x => ExtractDifference(currencyByType, x))
                .DistinctUntilChanged()
                .Subscribe(Reinitialize)
                .AddTo(Anchors);
        }

        private IDictionary<string, float> ExtractDifference(IDictionary<string, float> existingDictionary, IDictionary<string, float> candidate)
        {
            return candidate
                .Where(x => !existingDictionary.ContainsKey(x.Key) || Math.Abs(existingDictionary[x.Key] - x.Value) > float.Epsilon)
                .ToDictionary(x => x.Key, x => x.Value);
        } 

        private void Reinitialize(IDictionary<string, float> pricesConfig)
        {
            foreach (var kvp in pricesConfig)
            {
                currencyByType[kvp.Key] = kvp.Value;
            }
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