using PoeShared.Common;

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
            {KnownCurrencyNameList.BlessedOrb, "Blessed_Orb"},
            {KnownCurrencyNameList.CartographersChisel, "Cartographer's_Chisel"},
            {KnownCurrencyNameList.ChaosOrb, "Chaos_Orb"},
            {KnownCurrencyNameList.ChromaticOrb, "Chromatic_Orb"},
            {KnownCurrencyNameList.DivineOrb, "Divine_Orb"},
            {KnownCurrencyNameList.ExaltedOrb, "Exalted_Orb"},
            {KnownCurrencyNameList.GemcuttersPrism, "Gemcutter's_Prism"},
            {KnownCurrencyNameList.JewellersOrb, "Jeweller's_Orb"},
            {KnownCurrencyNameList.OrbOfAlchemy, "Orb_of_Alchemy"},
            {KnownCurrencyNameList.OrbOfAlteration, "Orb_of_Alteration"},
            {KnownCurrencyNameList.OrbOfChance, "Orb_of_Chance"},
            {KnownCurrencyNameList.OrbOfFusing, "Orb_of_Fusing"},
            {KnownCurrencyNameList.OrbOfRegret, "Orb_of_Regret"},
            {KnownCurrencyNameList.OrbOfScouring, "Orb_of_Scouring"},
            {KnownCurrencyNameList.RegalOrb, "Regal_Orb"}, 
            {KnownCurrencyNameList.VaalOrb, "Vaal_Orb"}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string))
            {
                return null;
            }

            var rawPrice = (string) value;
            var price = PriceToCurrencyConverter.Instance.Convert(rawPrice);
            if (price.IsEmpty)
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