using CsQuery.ExtensionMethods;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;

namespace PoeEye.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using Config;

    using Converters;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Scaffolding;

    using ReactiveUI;
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class PoePriceCalculcator : DisposableReactiveObject, IPoePriceCalculcator
    {
        private readonly IDictionary<string, float> currencyByType = new Dictionary<string, float>();

        public PoePriceCalculcator([NotNull] IPoeEyeMainConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(() => configProvider);

            configProvider
                .WhenChanged
                .Select(x => x.CurrenciesPriceInChaos)
                .Select(x => ExtractDifference(currencyByType, x))
                .DistinctUntilChanged()
                .Subscribe(Reinitialize)
                .AddTo(Anchors);
        }

        public PoePrice GetEquivalentInChaosOrbs(PoePrice price)
        {
            if (price.IsEmpty)
            {
                return PoePrice.Empty;
            }

            float currencyMultilplier;
            if (!currencyByType.TryGetValue(price.CurrencyType, out currencyMultilplier))
            {
                Log.Instance.Debug(
                    $"[PriceCalculcator] Could not convert currency type '{price.CurrencyType}' to multiplier, price: {price}\r\nMultipliers:{currencyByType.DumpToText()}");
                return PoePrice.Empty;
            }

            return new PoePrice(KnownCurrencyNameList.ChaosOrb, price.Value * currencyMultilplier);
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

            Log.Instance.Debug($"[PriceCalculcator] Currencies list:\r\n{currencyByType.DumpToText()}");
        }
    }
}