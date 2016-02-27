namespace PoeEyeUi.Converters
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
        private readonly Regex currencyParser = new Regex(@"(?'value'[\d\.\,]*)\s*(?'type'\w*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IConverter<string, PoePrice> Instance => instance.Value;

        public PoePrice Convert(string rawPrice)
        {
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

            return new PoePrice(currencyTypeString, currencyValue);
        }
    }
}