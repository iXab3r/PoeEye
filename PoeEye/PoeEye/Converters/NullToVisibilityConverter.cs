namespace PoeEye.Converters
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

    internal sealed class NullToVisibilityConverter : IValueConverter
    {
        public Visibility NullValue { get; set; } = Visibility.Collapsed;

        public Visibility NotNullValue { get; set; } = Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull;
            if (value is string)
            {
                isNull = IsNull(value as string);
            }
            else if (value is IList)
            {
                isNull = IsNull(value as IList);
            }
            else if (value is IEnumerable)
            {
                isNull = IsNull(value as IEnumerable);
            }
            else
            {
                isNull = value == null;
            }

            return isNull
                ? NullValue
                : NotNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private bool IsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        private bool IsNull(IList collection)
        {
            return collection == null || collection.Count <= 0;
        }

        private bool IsNull(IEnumerable collection)
        {
            return collection == null
                ? true
                : !collection.OfType<object>().Any();
        }
    }
}