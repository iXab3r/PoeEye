using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Blue.MVVM.Converter;

namespace PoeShared.Converters
{
    public sealed class NullToVisibilityMultiValueConverter : VisibilityConverterBase, IMultiValueConverter
    {
        public IBoolMultiValueConverterStrategy Strategy { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                return Binding.DoNothing;
            }

            var result = Convert(RequireStrategy(), values, parameter);
            return result ? TrueValue : FalseValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private IBoolMultiValueConverterStrategy RequireStrategy()
        {
            var strategy = Strategy;
            if (strategy != null)
            {
                return strategy;
            }

            throw new InvalidOperationException("no logical strategy has been set");
        }

        public bool Convert(IBoolMultiValueConverterStrategy strategy, object[] values, object parameter)
        {
            var nullCheckResults = values.Select(ConverterHelpers.IsNullOrEmpty).ToArray();
            return strategy.Convert(nullCheckResults, parameter);
        }
    }
}