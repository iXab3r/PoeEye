﻿using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace PoeShared.Converters;

public class FileInfoToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FileInfo valueFileInfo)
        {
            return valueFileInfo.FullName;
        }
        
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string valueString || string.IsNullOrEmpty(valueString))
        {
            return null;
        }

        return new FileInfo(valueString);
    }
}