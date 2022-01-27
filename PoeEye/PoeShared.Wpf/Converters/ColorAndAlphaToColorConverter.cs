using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PoeShared.Converters;

public sealed class ColorAndAlphaToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2)
        {
            return Binding.DoNothing;
        }

        if (values[0] is not Color color)
        {
            return Binding.DoNothing;
        }
        var alpha = System.Convert.ToByte(values[1]);

        return new Color()
        {
            A = alpha,
            R = color.R,
            G = color.G,
            B = color.B
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}