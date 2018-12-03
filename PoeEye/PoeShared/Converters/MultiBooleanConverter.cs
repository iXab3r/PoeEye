using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    internal sealed class MultiBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var result = false;
            foreach (var value in values)
            {
                if (value is bool)
                {
                    result |= (bool) value;
                }
            }

            return result;
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