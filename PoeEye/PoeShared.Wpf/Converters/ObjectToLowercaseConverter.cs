﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class ObjectToLowercaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString().ToLowerInvariant();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}