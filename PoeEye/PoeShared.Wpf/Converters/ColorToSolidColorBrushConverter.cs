using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PoeShared.Converters;

public class ColorToSolidColorBrushConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<Color, SolidColorBrush> BrushesByColor = new();
    private static readonly Lazy<ColorToSolidColorBrushConverter> InstanceSupplier = new();

    public static ColorToSolidColorBrushConverter Instance => InstanceSupplier.Value;

    public static SolidColorBrush Convert(Color color)
    {
        return BrushesByColor.GetOrAdd(color, x =>
        {
            var result = new SolidColorBrush(x);
            result.Freeze();
            return result;
        });
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
        {
            return Binding.DoNothing;
        }

        return Convert(color);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return ((SolidColorBrush) value)?.Color;
    }
}