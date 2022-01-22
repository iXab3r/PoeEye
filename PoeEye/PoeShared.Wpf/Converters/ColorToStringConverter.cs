using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PoeShared.Converters;

public class ColorToStringConverter : IValueConverter
{
    private readonly ColorConverter colorConverter = new ColorConverter();
        
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color valueColor)
        {
            return colorConverter.ConvertTo(valueColor, typeof(string));
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string valueString)
            {
                return colorConverter.ConvertFrom(valueString);
            }
            return Binding.DoNothing;
        }
        catch (Exception)
        {
            return Binding.DoNothing;
        }
    }
}