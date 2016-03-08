namespace PoeEye.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Windows.Data;

    internal sealed class PriceToImageUriConverter : IValueConverter
    {
        private const string ImagesPathPrefix = "pack://application:,,,/Resources/Currencies";

        private static readonly IDictionary<string, string> KnownImages = new Dictionary<string, string>
        {
            {"blessed", "Blessed_Orb"},
            {"chisel", "Cartographer's_Chisel"},
            {"chaos", "Chaos_Orb"},
            {"chromatic", "Chromatic_Orb"},
            {"divine", "Divine_Orb"},
            {"exalted", "Exalted_Orb"},
            {"gcp", "Gemcutter's_Prism"},
            {"jewellers", "Jeweller's_Orb"},
            {"alchemy", "Orb_of_Alchemy"},
            {"alteration", "Orb_of_Alteration"},
            {"chance", "Orb_of_Chance"},
            {"fusing", "Orb_of_Fusing"},
            {"regret", "Orb_of_Regret"},
            {"scouring", "Orb_of_Scouring"},
            {"regal", "Regal_Orb"}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string))
            {
                return null;
            }

            var rawPrice = (string) value;
            var price = PriceToCurrencyConverter.Instance.Convert(rawPrice);
            if (price == null)
            {
                return null;
            }

            string imageName;
            if (!KnownImages.TryGetValue(price.CurrencyType, out imageName))
            {
                return null;
            }

            return Path.Combine(ImagesPathPrefix, imageName + ".png");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}