using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public class NumericToStringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var doubleValue = System.Convert.ToDouble(value);
        return doubleValue switch
        {
            < 0.1 => "F2",
            < 1 => "F1",
            _ => "F0"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}