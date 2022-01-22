using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PoeShared.Converters;

internal sealed class BitmapImageOrNullConverter : IValueConverter
{
    public bool IsInverted { get; set; }
        
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var valueIsSupported = value is BitmapImage;
        valueIsSupported ^= IsInverted;
        return valueIsSupported ? value : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}