using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoeShared.Common;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeShared.Converters
{
    public sealed class PriceToCurrencyConverter : IConverter<string, PoePrice>
    {
        private static readonly Lazy<IConverter<string, PoePrice>> instance = new Lazy<IConverter<string, PoePrice>>(() => new PriceToCurrencyConverter());
        private static readonly Regex CurrencyParser = new Regex(@"^[~]?(?:b\/o |price )?(?'value'[\d\.\,]+) ?(?'type'[\w ]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly IDictionary<string, string> DefaultCurrencyByAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Orb of Alteration", KnownCurrencyNameList.OrbOfAlteration},
            { "Alt", KnownCurrencyNameList.OrbOfAlteration},

            { "Blessed Orb", KnownCurrencyNameList.BlessedOrb},
            { "Blessed", KnownCurrencyNameList.BlessedOrb},

            { "Cartographer's Chisel", KnownCurrencyNameList.CartographersChisel},
            { "Cartographers Chisel", KnownCurrencyNameList.CartographersChisel},
            { "Chisel", KnownCurrencyNameList.CartographersChisel},
              
            { "Chaos Orb", KnownCurrencyNameList.ChaosOrb},
            { "Chaos", KnownCurrencyNameList.ChaosOrb},
              
            { "Chromatic Orb", KnownCurrencyNameList.ChromaticOrb},
            { "Chrome", KnownCurrencyNameList.ChromaticOrb},
            { "Chrom", KnownCurrencyNameList.ChromaticOrb},
              
            { "Divine Orb", KnownCurrencyNameList.DivineOrb},
            { "Divine", KnownCurrencyNameList.DivineOrb},
              
            { "Exalted Orb", KnownCurrencyNameList.ExaltedOrb},
            { "Exalted", KnownCurrencyNameList.ExaltedOrb},
            { "Ex", KnownCurrencyNameList.ExaltedOrb},
            { "Exa", KnownCurrencyNameList.ExaltedOrb},
              
            { "Gemcutter's Prism", KnownCurrencyNameList.GemcuttersPrism},
            { "Gemcutters Prism", KnownCurrencyNameList.GemcuttersPrism},
            { "Gcp", KnownCurrencyNameList.GemcuttersPrism},
              
            { "Jewellers Orb", KnownCurrencyNameList.JewellersOrb},
            { "Jew", KnownCurrencyNameList.JewellersOrb},
              
            { "Orb of Alchemy", KnownCurrencyNameList.OrbOfAlchemy},
            { "Alch", KnownCurrencyNameList.OrbOfAlchemy},
              
            { "Orb of Fusing", KnownCurrencyNameList.OrbOfFusing},
            { "Fuse", KnownCurrencyNameList.OrbOfFusing},

            { "Orb of Chance", KnownCurrencyNameList.OrbOfChance},
            { "Chance", KnownCurrencyNameList.OrbOfChance},

            { "Orb of Regret", KnownCurrencyNameList.OrbOfRegret},
            { "Regret", KnownCurrencyNameList.OrbOfRegret},

            { "Orb of Scouring", KnownCurrencyNameList.OrbOfScouring},
            { "Scour", KnownCurrencyNameList.OrbOfScouring},

            { "Regal Orb", KnownCurrencyNameList.RegalOrb},
            { "Regal", KnownCurrencyNameList.RegalOrb},

            { "Vaal Orb", KnownCurrencyNameList.VaalOrb},
            { "Vaal", KnownCurrencyNameList.VaalOrb},

            { "Mirror of Kalandra", KnownCurrencyNameList.MirrorOfKalandra},
            { "Mirror", KnownCurrencyNameList.MirrorOfKalandra},

            { "Eternal Orb", KnownCurrencyNameList.EternalOrb},
            { "Eternal", KnownCurrencyNameList.EternalOrb},

            { "Unknown", KnownCurrencyNameList.Unknown},
        };

        public static IConverter<string, PoePrice> Instance => instance.Value;

        private readonly ConcurrentDictionary<string, string> currencyByAlias;

        public PriceToCurrencyConverter()
        {
            currencyByAlias = new ConcurrentDictionary<string, string>(DefaultCurrencyByAlias, StringComparer.OrdinalIgnoreCase);
            DefaultCurrencyByAlias.Values.ForEach(x => currencyByAlias[x] = x);

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