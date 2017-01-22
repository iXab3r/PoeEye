using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Scaffolding.WPF.Converters
{
    internal sealed class NullToVisibilityConverter : IValueConverter
    {
        public Visibility NullValue { get; set; } = Visibility.Collapsed;

        public Visibility NotNullValue { get; set; } = Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = ConverterHelpers.IsNullOrEmpty(value);

            return isNull
                ? NullValue
                : NotNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}