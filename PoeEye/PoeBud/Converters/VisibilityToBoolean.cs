using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeBud.Converters
{
    internal sealed class VisibilityToBoolean : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? TrueValue : FalseValue;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (Visibility)value == TrueValue;
            }

            return Binding.DoNothing;
        }
    }
}