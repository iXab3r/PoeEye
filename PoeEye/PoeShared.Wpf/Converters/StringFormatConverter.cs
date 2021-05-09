using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public class StringFormatConverter : IValueConverter, IMultiValueConverter
    {
        public string FixedFormat { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var format = FixedFormat ?? System.Convert.ToString(parameter);
            return string.Format(culture, format, values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var format = FixedFormat ?? System.Convert.ToString(parameter);
            return string.Format(culture, format, value);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Convert(value, targetType, parameter, new CultureInfo(language));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}