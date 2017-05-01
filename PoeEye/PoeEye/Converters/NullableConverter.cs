﻿using System.Windows;

namespace PoeEye.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal sealed class NullableConverter : IValueConverter
    {
        public static IValueConverter Instance = new NullableConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        private object Convert(object value, Type targetType)
        {
            if (targetType == null || targetType == typeof(string))
            {
                return value;
            }

            var raw = value as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return GetDefaultValue(targetType);
            }

            return value;
        }

        public object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }
    }
}