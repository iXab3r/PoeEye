using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class FlagsConverter : IValueConverter
{
    public object TrueValue { get; set; }

    public object FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enumValue = System.Convert.ToUInt64(value);
        var flagValue = System.Convert.ToUInt64(parameter);
        var hasFlag = (flagValue & enumValue) == flagValue;
        return hasFlag ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}