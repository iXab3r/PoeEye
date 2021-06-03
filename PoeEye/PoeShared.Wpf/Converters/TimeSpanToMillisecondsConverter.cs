using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class TimeSpanToMillisecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
            {
                return DependencyProperty.UnsetValue;
            }

            var timeSpan = (TimeSpan)value;

            return timeSpan.TotalMilliseconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return TimeSpan.FromMilliseconds(System.Convert.ToDouble(value));
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}