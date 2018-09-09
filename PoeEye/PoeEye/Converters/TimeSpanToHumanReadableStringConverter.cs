using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeEye.Converters
{
    internal sealed class TimeSpanToHumanReadableStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan timeSpan;
            if (value is TimeSpan)
            {
                timeSpan = (TimeSpan) value;
            }
            else if (value is TimeSpan?)
            {
                timeSpan = ((TimeSpan?)value).Value;
            }
            else
            {
                return Binding.DoNothing;
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
            return $"{timeSpan.TotalSeconds:F0}s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}