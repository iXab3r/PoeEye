using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class TimeSpanToMillisecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
            {
                return value;
            }

            var timeSpan = (TimeSpan)value;

            return (int)timeSpan.TotalMilliseconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                return TimeSpan.FromMilliseconds((int)value);
            }

            if (value is double)
            {
                return TimeSpan.FromMilliseconds((double)value);
            }

            return Binding.DoNothing;
        }
    }
}