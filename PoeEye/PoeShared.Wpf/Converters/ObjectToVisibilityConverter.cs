using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class ObjectToVisibilityConverter : IValueConverter
{
    public Visibility? TrueValue { get; set; }

    public Visibility? FalseValue { get; set; }

    public object CompareTo { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = Equals(value, CompareTo)
            ? TrueValue
            : FalseValue;
        return result ?? Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}