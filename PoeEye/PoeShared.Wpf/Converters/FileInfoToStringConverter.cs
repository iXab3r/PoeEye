using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace PoeShared.Converters;

public class FileInfoToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FileInfo valueFileInfo)
        {
            return Binding.DoNothing;
        }

        return valueFileInfo.FullName;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string valueString || string.IsNullOrEmpty(valueString))
        {
            return Binding.DoNothing;
        }

        return new FileInfo(valueString);
    }
}