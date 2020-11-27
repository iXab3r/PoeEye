using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class DataSizeToHumanReadableStringConverter : IValueConverter
    {
        private const long Kilobyte = 1024;
        private const long Megabyte = Kilobyte * 1024;
        private const long Gigabyte = Megabyte * 1024;
        private const long Terabyte = Gigabyte * 1024;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueToConvert = System.Convert.ToDouble(value);

            return valueToConvert switch
            {
                (> Terabyte) => $"{valueToConvert / Terabyte:F3} TB",
                (> Gigabyte) => $"{valueToConvert / Gigabyte:F2} GB",
                (> Megabyte) => $"{valueToConvert / Megabyte:F1} MB",
                (> Kilobyte) => $"{valueToConvert / Kilobyte} KB",
                _ =>  $"{valueToConvert}B"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}