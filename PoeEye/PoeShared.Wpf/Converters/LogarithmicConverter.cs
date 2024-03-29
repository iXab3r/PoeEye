﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public class LogarithmicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var progressValue = System.Convert.ToDouble(value);
        return progressValue > 0 ? Math.Log(progressValue * 9 + 1, 10) : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var logarithmicValue = System.Convert.ToDouble(value);
        return logarithmicValue > 0 ? (Math.Pow(10, logarithmicValue) - 1) / 9 : 0;
    }
}