using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PoeShared.Converters;

public class ColorToStringConverter : DependencyObject, IValueConverter
{
    private readonly ColorConverter colorConverter = new ColorConverter();

    public static readonly DependencyProperty AllowAlphaProperty = DependencyProperty.Register(
        "AllowAlpha", typeof(bool), typeof(ColorToStringConverter), new PropertyMetadata(true));

    public bool AllowAlpha
    {
        get { return (bool) GetValue(AllowAlphaProperty); }
        set { SetValue(AllowAlphaProperty, value); }
    }
        
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
            if (value == null)
            {
                return Binding.DoNothing;
            }
            if (value is string valueString)
            {
                var colorString = valueString.Trim('#');
                if (AllowAlpha && colorString.Length != 8)
                {
                    throw new FormatException($"Color must be in format #AARRGGBB");
                }

                if (!AllowAlpha && colorString.Length != 6)
                {
                    throw new FormatException($"Color must be in format #RRGGBB");
                }
                
                return colorConverter.ConvertFrom(valueString);
            }
            throw new FormatException($"Supplied data of type {value.GetType()} is not supported");
        }
        catch (Exception)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}