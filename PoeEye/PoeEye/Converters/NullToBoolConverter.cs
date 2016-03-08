namespace PoeEye.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal sealed class NullToBoolConverter : IValueConverter
    {
        public bool NullValue { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? NullValue : !NullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}