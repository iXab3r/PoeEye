using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public class MinMaxSideIntToDoubleConverter : IValueConverter
{
    private static readonly Lazy<MinMaxSideIntToDoubleConverter> instanceSupplier = new();

    public static MinMaxSideIntToDoubleConverter Instance => instanceSupplier.Value;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            int valueInt => valueInt <= 0 ? double.NaN : valueInt,
            double valueDouble => valueDouble,
            _ => Binding.DoNothing
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}