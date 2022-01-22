using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters;

public sealed class FirstOrDefaultMultiValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.EmptyIfNull().FirstOrDefault(x => x != null);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}