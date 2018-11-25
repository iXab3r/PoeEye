﻿using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using Common.Logging;
using PoeShared.Common;

namespace PoeShared.Converters
{
    public sealed class PriceToImageUriConverter : IValueConverter
    {
        private const string ImagesPathPrefix = "Resources/Currencies";
        private static readonly ILog Log = LogManager.GetLogger(typeof(PriceToImageUriConverter));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string))
            {
                return null;
            }

            var rawPrice = (string)value;
            var price = StringToPoePriceConverter.Instance.Convert(rawPrice);
            if (price.IsEmpty)
            {
                Log.Debug($"Failed to convert string '{rawPrice}' to PoePrice");
                return null;
            }

            string imageName;
            if (!KnownCurrencyNameList.KnownImages.TryGetValue(price.CurrencyType, out imageName))
            {
                imageName = KnownCurrencyNameList.KnownImages[KnownCurrencyNameList.Unknown];
            }

            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagesPathPrefix, imageName + ".png");
            if (!File.Exists(imagePath))
            {
                Log.Debug($"Failed to find image for {price}, got path {imagePath}");
                return null;
            }

            return imagePath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}