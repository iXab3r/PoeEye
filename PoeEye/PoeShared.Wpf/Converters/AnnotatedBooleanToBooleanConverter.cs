using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters
{
    public class AnnotatedBooleanToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean booleanValue)
            {
                return booleanValue;
            } else if (value is AnnotatedBoolean annotatedBoolean)
            {
                return (bool)annotatedBoolean;
            }

            throw new NotSupportedException($"Value type is not supported: {value}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean booleanValue)
            {
                return new AnnotatedBoolean(booleanValue, $"Converted by WPF from {value}");
            } else if (value is AnnotatedBoolean annotatedBoolean)
            {
                return annotatedBoolean;
            }

            throw new NotSupportedException($"Value type is not supported: {value}");
        }
    }
}