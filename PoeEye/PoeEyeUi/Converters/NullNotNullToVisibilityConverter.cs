namespace PoeEyeUi.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    internal sealed class NullNotNullToVisibilityConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = value is string ? string.IsNullOrWhiteSpace(value as string) : value == null;

            if (IsInverted)
            {
                return isNull
                       ? Visibility.Visible
                       : Visibility.Collapsed;
            }
            else
            {
                return isNull
                       ? Visibility.Collapsed
                       : Visibility.Visible;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}