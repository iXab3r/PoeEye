using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding.WPF.Converters;

namespace PoeShared.Converters
{
    internal sealed class NullToBoolConverter : IValueConverter
    {
        public bool NullValue { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelpers.IsNullOrEmpty(value) ? NullValue : !NullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}