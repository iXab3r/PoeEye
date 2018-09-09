using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeBud.Converters
{
    internal sealed class BooleanToObjectConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Boolean))
            {
                return Binding.DoNothing;
            }

            var boolValue = (Boolean)value;
            return boolValue ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}