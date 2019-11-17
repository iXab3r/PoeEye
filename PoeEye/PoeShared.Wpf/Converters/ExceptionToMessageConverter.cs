using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public sealed class ExceptionToMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var exception = value as Exception;
            if (exception == null)
            {
                return null;
            }

            return exception.Message;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}