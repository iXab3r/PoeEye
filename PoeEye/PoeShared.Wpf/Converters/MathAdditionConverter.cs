using System;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class MathAdditionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        parameter = ConvertToNumberType(parameter);

        if (value is int && parameter is int)
        {
            return (int)value + (int)parameter;
        }

        if (value is double && parameter is double)
        {
            return (double)value + (double)parameter;
        }

        if (value is double && parameter is int)
        {
            return (double)value + (int)parameter;
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int && parameter is int)
        {
            return (int)value - (int)parameter;
        }

        if (value is double && parameter is double)
        {
            return (double)value - (double)parameter;
        }

        if (value is double && parameter is int)
        {
            return (double)value - (int)parameter;
        }

        return Binding.DoNothing;
    }

    private object ConvertToNumberType(object parameter)
    {
        if (!(parameter is string))
        {
            return parameter;
        }

        var stringParameter = (string)parameter;

        int parsedInt;
        if (int.TryParse(stringParameter, out parsedInt))
        {
            return parsedInt;
        }

        double parsedDouble;
        if (double.TryParse(stringParameter, out parsedDouble))
        {
            return parsedDouble;
        }

        return parameter;
    }
}