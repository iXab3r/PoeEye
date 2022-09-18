using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters;

public class StringToMidConverter : IValueConverter
{
    public int MaxCharCount { get; set; } = 32;

    public bool AddSuffix { get; set; } = false;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = System.Convert.ToString(value);
        return str.TakeMidChars(MaxCharCount, AddSuffix);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}