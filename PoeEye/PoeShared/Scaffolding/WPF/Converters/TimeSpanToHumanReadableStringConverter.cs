using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Scaffolding.WPF.Converters
{
    public sealed class TimeSpanToHumanReadableStringConverter : IValueConverter
    {
        //FIXME Remove duplicate converters
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
            {
                return value;
            }

            var timeSpan = (TimeSpan)value;

            if (timeSpan.TotalHours > 24)
            {
                return $"{timeSpan.TotalDays:F0}d {timeSpan.Hours:F0}h";
            }

            if (timeSpan.TotalMinutes > 120)
            {
                return $"{timeSpan.TotalHours:F0}h";
            }

            if (timeSpan.TotalSeconds > 120)
            {
                return $"{timeSpan.TotalMinutes:F0}m";
            }

            return $"{timeSpan.TotalSeconds:F0}s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}