using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class SizeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            WpfSize wpfSize => $"{wpfSize.Width}x{wpfSize.Height}",
            WinSize winSize => $"{winSize.Width}x{winSize.Height}",
            _ => Binding.DoNothing
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}