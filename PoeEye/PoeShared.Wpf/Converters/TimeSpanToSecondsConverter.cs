using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is TimeSpan))
        {
            return DependencyProperty.UnsetValue;
        }

        var timeSpan = (TimeSpan)value;

        return (int)timeSpan.TotalSeconds;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int)
        {
            return TimeSpan.FromSeconds((int)value);
        }

        if (value is double)
        {
            return TimeSpan.FromSeconds((double)value);
        }

        return DependencyProperty.UnsetValue;
    }
}