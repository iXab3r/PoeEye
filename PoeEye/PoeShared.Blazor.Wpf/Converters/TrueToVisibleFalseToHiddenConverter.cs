using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Blazor.Wpf.Converters;

internal sealed class TrueToVisibleFalseToHiddenConverter : IValueConverter
{
    public static readonly TrueToVisibleFalseToHiddenConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        return flag ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}