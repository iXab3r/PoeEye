using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.Models
{
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class PoePriceCalculator : DisposableReactiveObject, IPoePriceCalculcator
    {
        private static readonly ILog Log = LogManager.GetLogger<PoePriceCalculator>();

        private readonly IDictionary<string, float> currencyByType = new Dictionary<string, float>();

        public PoePriceCalculator([NotNull] IPoeEyeMainConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            configProvider
                .WhenChanged
                .Select(x => x.CurrenciesPriceInChaos)
                .Select(x => ExtractDifference(currencyByType, x))
                .DistinctUntilChanged()
                .Subscribe(Reinitialize)
                .AddTo(Anchors);

            WhenChanged = configProvider.WhenChanged.ToUnit();
        }

        public IObservable<Unit> WhenChanged { get; }

        public PoePrice GetEquivalentInChaosOrbs(PoePrice price)
        {
            if (price.IsEmpty)
            {
                return PoePrice.Empty;
            }

            if (!currencyByType.TryGetValue(price.CurrencyType, out var currencyMultiplier))
            {
                Log.Debug(
                    $"[PriceCalculator] Could not convert currency type '{price.CurrencyType}' to multiplier, price: {price}\r\nMultipliers:{currencyByType.DumpToTextRaw()}");
                return PoePrice.Empty;
            }

            return new PoePrice(KnownCurrencyNameList.ChaosOrb, price.Value * currencyMultiplier);
        }

        public bool CanConvert(PoePrice price)
        {
            return currencyByType.ContainsKey(price.CurrencyType);
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

            if (!currencyByType.ContainsKey(KnownCurrencyNameList.ChaosOrb))
            {
                Log.Debug($"[PriceCalculator] Chaos orb is not in a list of prices, adding it");
                currencyByType[KnownCurrencyNameList.ChaosOrb] = 1.0f;
            }

            Log.Debug($"[PriceCalculator] Currencies list:\r\n{currencyByType.DumpToText()}");
        }
    }
}