using System.Collections.Concurrent;
using System.Collections.Generic;
using PoeShared.Scaffolding;

namespace PoeEye.Converters
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using PoeShared;
    using PoeShared.Common;

    using TypeConverter;

    internal sealed class PriceToCurrencyConverter : IConverter<string, PoePrice>
    {
        private static readonly Lazy<IConverter<string, PoePrice>> instance = new Lazy<IConverter<string, PoePrice>>(() => new PriceToCurrencyConverter());
        private static readonly Regex CurrencyParser = new Regex(@"^[~]?(?:b\/o |price )?(?'value'[\d\.\,]+) ?(?'type'[\w ]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly IDictionary<string, string> DefaultCurrencyByAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Orb of Alteration", KnownCurrencyNameList.OrbOfAlteration},
            {"Blessed Orb", KnownCurrencyNameList.BlessedOrb},
            {"Cartographer's Chisel", KnownCurrencyNameList.CartographersChisel},
            {"Chaos Orb", KnownCurrencyNameList.ChaosOrb},
            {"Chromatic Orb", KnownCurrencyNameList.ChromaticOrb},
            {"Divine Orb", KnownCurrencyNameList.DivineOrb},
            {"Exalted Orb", KnownCurrencyNameList.ExaltedOrb},
            {"Gemcutter's Prism", KnownCurrencyNameList.GemcuttersPrism},
            {"Jewellers Orb", KnownCurrencyNameList.JewellersOrb},
            {"Orb of Fusing", KnownCurrencyNameList.OrbOfFusing},
            {"Orb of Chance", KnownCurrencyNameList.OrbOfChance},
            {"Orb of Regret", KnownCurrencyNameList.OrbOfRegret},
            {"Orb of Scouring", KnownCurrencyNameList.OrbOfScouring},
            {"Regal Orb", KnownCurrencyNameList.RegalOrb},
            {"Vaal Orb", KnownCurrencyNameList.VaalOrb},
        };

        public static IConverter<string, PoePrice> Instance => instance.Value;


        private readonly ConcurrentDictionary<string, string> currencyByAlias = new ConcurrentDictionary<string, string>(DefaultCurrencyByAlias);

        public PriceToCurrencyConverter()
        {
            Log.Instance.Debug($"[PriceToCurrencyConverter..ctor] Aliases list:\r\n{currencyByAlias.DumpToText()}");
        }

        public PoePrice Convert(string rawPrice)
        {
            if (String.IsNullOrWhiteSpace(rawPrice))
            {
                return PoePrice.Empty;
            }
            var match = CurrencyParser.Match(rawPrice);
            if (!match.Success)
            {
                return PoePrice.Empty;
            }

            var currencyValueString = match.Groups["value"].Value;
            var currencyTypeOrAliasString = match.Groups["type"].Value;

            float currencyValue;
            if (!float.TryParse(currencyValueString, NumberStyles.Any, CultureInfo.InvariantCulture, out currencyValue))
            {
                Log.Instance.Debug(
                    $"[PriceCalculcator] Could not convert value '{currencyValueString}' to float, rawPrice: {rawPrice}");
                return PoePrice.Empty;
            }

            string currencyType;
            if (!currencyByAlias.TryGetValue(currencyTypeOrAliasString, out currencyType))
            {
                currencyType = currencyTypeOrAliasString;
            }

            return new PoePrice(currencyType, currencyValue);
        }
    }
}