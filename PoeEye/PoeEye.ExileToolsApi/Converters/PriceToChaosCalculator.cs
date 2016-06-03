using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PoeShared;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.ExileToolsApi.Converters
{
    internal sealed class PriceToChaosCalculator : IPoePriceCalculcator
    {
        /* ExileTools uses fixed rates for price conversion */

        private readonly IDictionary<string, float> currencyByType =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                { KnownCurrencyNameList.OrbOfAlteration, 0.062f},
                { KnownCurrencyNameList.ChromaticOrb, 0.067f},
                { KnownCurrencyNameList.JewellersOrb, 0.125f},
                { KnownCurrencyNameList.OrbOfChance, 0.143f},
                { KnownCurrencyNameList.CartographersChisel, 0.333f},
                { KnownCurrencyNameList.OrbOfAlchemy, 0.500f},
                { KnownCurrencyNameList.OrbOfFusing, 0.500f},
                { KnownCurrencyNameList.OrbOfScouring, 0.500f},
                { KnownCurrencyNameList.BlessedOrb, 0.750f},
                { KnownCurrencyNameList.VaalOrb, 1.000f},
                { KnownCurrencyNameList.ChaosOrb, 1.000f},
                { KnownCurrencyNameList.OrbOfRegret, 1.000f},
                { KnownCurrencyNameList.GemcuttersPrism, 2.000f},
                { KnownCurrencyNameList.RegalOrb, 2.000f},
                { KnownCurrencyNameList.DivineOrb, 17.000f},
                { KnownCurrencyNameList.ExaltedOrb, 80.000f},
                { KnownCurrencyNameList.MirrorOfKalandra, 5000.000f},
                { KnownCurrencyNameList.EternalOrb, 10000.000f },
            };

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
    }
}