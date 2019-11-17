using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class EqualityConverter : IMultiValueConverter
    {
        public object TrueValue { get; set; }
        
        public object FalseValue { get; set; }
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;

            var valueToCompare = values[0];
            var result = values.All(x => Equals(x, valueToCompare));

            return result ? TrueValue : FalseValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}