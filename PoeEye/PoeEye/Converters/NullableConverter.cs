using System.Windows;

namespace PoeEye.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal sealed class NullableConverter : IValueConverter
    {
        public static IValueConverter Instance = new NullableConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}