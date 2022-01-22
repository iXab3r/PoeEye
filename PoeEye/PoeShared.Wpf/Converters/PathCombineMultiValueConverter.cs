using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class PathCombineMultiValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null)
        {
            return Binding.DoNothing;
        }
            
        return Path.Combine(values.OfType<string>().ToArray());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return value switch
        {
            string s => s.Split(Path.DirectorySeparatorChar),
            null => new object[0],
            _ => throw new NotSupportedException($"{nameof(ConvertBack)} for value {value} of type {value.GetType()} is not supported")
        };
    }
}