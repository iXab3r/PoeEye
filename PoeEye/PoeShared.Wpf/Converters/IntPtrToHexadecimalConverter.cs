using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters
{
    public sealed class IntPtrToHexadecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IntPtr)
            {
                return ((IntPtr) value).ToHexadecimal();
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}