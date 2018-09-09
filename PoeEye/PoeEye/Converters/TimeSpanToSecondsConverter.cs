using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeEye.Converters
{
    internal sealed class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
            {
                return value;
            }
            var timeSpan = (TimeSpan) value;

            return (int) timeSpan.TotalSeconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                return TimeSpan.FromSeconds((int) value);
            }

            if (value is double)
            {
                return TimeSpan.FromSeconds((double) value);
            }

            return Binding.DoNothing;
        }
    }
}