using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class TimeSpanToHumanReadableStringConverter : IValueConverter
    {
        //FIXME Remove duplicate converters
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                var ts = TimeSpan.FromSeconds((double) value);
                return Convert(ts, typeof(string), null, CultureInfo.InvariantCulture);
            }

            if (!(value is TimeSpan))
            {
                return value;
            }

            var timeSpan = (TimeSpan) value;

            if (timeSpan == TimeSpan.MaxValue)
            {
                return "∞";
            }

            if (timeSpan == TimeSpan.MinValue)
            {
                return "-∞";
            }

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

            if (timeSpan.TotalSeconds < 10)
            {
                return $"{timeSpan.TotalSeconds:F1}s";
            }

            return $"{timeSpan.TotalSeconds:F0}s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}