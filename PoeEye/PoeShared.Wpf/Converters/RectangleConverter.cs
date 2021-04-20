using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters
{
    internal sealed class RectangleConverter : IMultiValueConverter
    {
        private static readonly Rect Empty = new Rect(0, 0, 0, 0);
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != 2 && values.Length != 4)
            {
                throw new ArgumentException("Invalid values length, expected 2(Width,Height) or 4(X,Y,Width,Height)");
            }

            var dimensions = values.Select(System.Convert.ToDouble).ToArray();
            if (dimensions.Any(x => !double.IsFinite(x)))
            {
                return Empty;
            }
            return dimensions.Length == 2 
                ? new Rect(0, 0, Math.Max(dimensions[0], 0), Math.Max(dimensions[1], 0)) 
                : new Rect(dimensions[0], dimensions[1], Math.Max(dimensions[2], 0), Math.Max(dimensions[3], 0));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}