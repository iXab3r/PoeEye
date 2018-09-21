﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using Common.Logging;
using PoeShared.Common;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeShared.Converters
{
    public sealed class StringToPoePriceConverter : IConverter<string, PoePrice>, IValueConverter
    {
        private static readonly ILog Log = LogManager.GetLogger<StringToPoePriceConverter>();

        private static readonly Lazy<IConverter<string, PoePrice>> InstanceSupplier =
            new Lazy<IConverter<string, PoePrice>>(() => new StringToPoePriceConverter());

        private static readonly Regex CurrencyParser = new Regex(@"^[~]?(?:b\/o |price )?(?'value'[\d\.\,]+)? ?(?'type'[\w \-\']+)$",
                                                                 RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ConcurrentDictionary<string, string> currencyByAlias;

        public StringToPoePriceConverter()
        {
            currencyByAlias = new ConcurrentDictionary<string, string>(KnownCurrencyNameList.CurrencyByAlias, StringComparer.OrdinalIgnoreCase);

            Log.Debug($"[PriceToCurrencyConverter..ctor] Aliases list:\r\n{currencyByAlias.DumpToText()}");
        }

        public static IConverter<string, PoePrice> Instance => InstanceSupplier.Value;

        public PoePrice Convert(string rawPrice)
        {
            if (string.IsNullOrWhiteSpace(rawPrice))
            {
                return PoePrice.Empty;
            }

            rawPrice = rawPrice.Trim();
            if (KnownCurrencyNameList.CurrencyByAlias.ContainsKey(rawPrice))
            {
                return new PoePrice(KnownCurrencyNameList.CurrencyByAlias[rawPrice], 0);
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
                Log.Debug(
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rawPrice = value as string;
            if (rawPrice == null)
            {
                return Binding.DoNothing;
            }

            return Convert(rawPrice);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}