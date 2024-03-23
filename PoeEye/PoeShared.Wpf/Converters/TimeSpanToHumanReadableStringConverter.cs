using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class TimeSpanToHumanReadableStringConverter : IValueConverter
{
    private static readonly Lazy<TimeSpanToHumanReadableStringConverter> InstanceSupplier = new();

    public static TimeSpanToHumanReadableStringConverter Instance => InstanceSupplier.Value;

    public string Convert(TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.MaxValue)
        {
            return "∞";
        }

        if (timeSpan == TimeSpan.MinValue)
        {
            return "-∞";
        }

        if (timeSpan == TimeSpan.Zero)
        {
            return "0s";
        }

        if (timeSpan.TotalSeconds < 1)
        {
            return $"{timeSpan.TotalMilliseconds:F0}ms";
        }

        if (timeSpan.TotalHours > 24)
        {
            return $"{Math.Truncate(timeSpan.TotalDays):F0}d {timeSpan.Hours:F0}h";
        }

        if (timeSpan.TotalMinutes > 120)
        {
            return $"{timeSpan.TotalHours:F0}h";
        }

        if (timeSpan.TotalMinutes > 9)
        {
            return $"{timeSpan.TotalMinutes:F0}m";
        }

        if (timeSpan.TotalSeconds > 120)
        {
            return $"{Math.Truncate(timeSpan.TotalMinutes):F0}m{timeSpan.Seconds:F0}s";
        }

        if (timeSpan.TotalSeconds < 10)
        {
            return $"{timeSpan.TotalSeconds:F1}s";
        }

        return $"{timeSpan.TotalSeconds:F0}s";
    }

    //FIXME Remove duplicate converters
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double)
        {
            var ts = TimeSpan.FromSeconds((double) value);
            return Convert(ts, typeof(string), null, CultureInfo.InvariantCulture);
        }

        if (!(value is TimeSpan timeSpan))
        {
            return value;
        }

        return Convert(timeSpan);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}