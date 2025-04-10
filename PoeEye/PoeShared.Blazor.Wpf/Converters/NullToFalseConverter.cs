using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Blazor.Wpf.Converters;

internal sealed class NullToFalseConverter : IValueConverter
{
    public static readonly NullToFalseConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}