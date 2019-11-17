using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public class TwoNumbersToSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                return Binding.DoNothing;
            }

            if (values[0] is float && values[1] is float)
            {
                var width = (float) values[0];
                var height = (float) values[1];

                return new Size(width, height);
            }

            if (values[0] is double && values[1] is double)
            {
                var width = (double) values[0];
                var height = (double) values[1];

                return new Size(width, height);
            }

            if (values[0] is int && values[1] is int)
            {
                var width = (int) values[0];
                var height = (int) values[1];

                return new Size(width, height);
            }

            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}