using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Point = System.Windows.Point;
using WinPoint = System.Drawing.Point;
namespace PoeShared.Converters;

public sealed class PointToThicknessConverter : IValueConverter
{
    public static readonly PointToThicknessConverter Instance = new();
        
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            Point wpfPoint => new Thickness(wpfPoint.X, wpfPoint.Y, 0, 0),
            WinPoint winPoint => new Thickness(winPoint.X, winPoint.Y, 0, 0),
            _ => Binding.DoNothing
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}