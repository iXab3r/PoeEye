using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public class MultilineToSinglelineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Replace(Environment.NewLine, " ").Replace('\n', ' ').Replace('\r', ' ');
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}