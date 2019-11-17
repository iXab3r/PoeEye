using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class MultiBooleanAndConverter : IMultiValueConverter
    {
        public object TrueValue { get; set; }
        
        public object FalseValue { get; set; }
        
        public object Convert(object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Guard.ArgumentNotNull(values, nameof(values));
            var result = values.OfType<bool>().All(x => x);
            return result
                ? TrueValue
                : FalseValue;
        }

        public object[] ConvertBack(object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}