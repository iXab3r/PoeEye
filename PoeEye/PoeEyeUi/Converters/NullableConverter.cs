﻿namespace PoeEyeUi.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal sealed class NullableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                return null;
            }

            return value;
        }
    }
}