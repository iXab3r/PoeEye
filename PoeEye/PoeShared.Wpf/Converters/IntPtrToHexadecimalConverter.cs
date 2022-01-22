using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters;

public sealed class IntPtrToHexadecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IntPtr ptr)
        {
            return ptr.ToHexadecimal();
        } else if (value is int intPtr)
        {
            return intPtr.ToHexadecimal();
        } else if (value is uint uintPtr)
        {
            return uintPtr.ToHexadecimal();
        } else if (value is long longPtr)
        {
            return longPtr.ToHexadecimal();
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}