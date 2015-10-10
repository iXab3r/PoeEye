namespace PoeEyeUi.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal sealed class TimeSpanToHumanReadableStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
            {
                return value;
            }
            var timeSpan = (TimeSpan)value;

            if (timeSpan.TotalMinutes > 120)
            {
                return $"{timeSpan:%h}h";
            }
            else if (timeSpan.TotalSeconds > 120)
            {
                return $"{timeSpan:%m}m";
            }
            else
            {
                return $"{timeSpan:%s}s";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}