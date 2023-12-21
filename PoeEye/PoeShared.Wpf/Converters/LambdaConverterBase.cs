using System;
using System.Globalization;
using System.Windows.Data;
using PoeShared.Scaffolding;

namespace PoeShared.Converters;

public abstract class LambdaConverterBase<TIn, TOut> : IValueConverter
{
    protected abstract TOut Convert(TIn input);
    protected abstract TIn ConvertBack(TOut input);
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            TIn input => Convert(input),
            TOut output => ConvertBack(output),
            _ => Binding.DoNothing
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            TIn input => Convert(input),
            TOut output => ConvertBack(output),
            _ => Binding.DoNothing
        };
    }
}