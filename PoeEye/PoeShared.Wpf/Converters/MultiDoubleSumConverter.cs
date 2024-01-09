using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace PoeShared.Converters;

public sealed class MultiplicationConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length <= 0)
        {
            return Binding.DoNothing;
        }

        if (values.Length == 1)
        {
            return values[0];
        }

        var result = System.Convert.ToDouble(values[0]);
        result = values.Skip(1).Select(System.Convert.ToDouble).Aggregate(result, (res, x) => res *= x);
        return values[0] switch
        {
            double _ => result,
            float _ => (float) result,
            int _ => (int) result,
            long _ => (long) result,
            _ => throw new ArgumentOutOfRangeException($"Unknown first argument type: {values[0]} ({values[0].GetType()})")
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
    
public sealed class MultiDoubleSumConverter : IMultiValueConverter
{
    private static readonly Lazy<MultiDoubleSumConverter> InstanceSupplier = new(() => new MultiDoubleSumConverter());
    public static MultiDoubleSumConverter Instance => InstanceSupplier.Value;
    
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null)
        {
            return Binding.DoNothing;
        }

        return values.OfType<double>().Aggregate(0d, (i, d) => i + d);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}