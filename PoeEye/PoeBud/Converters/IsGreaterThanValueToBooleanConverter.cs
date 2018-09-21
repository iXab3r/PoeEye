using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeBud.Converters
{
    internal sealed class IsGreaterThanValueToBooleanConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            "MinValue",
            typeof(IComparable),
            typeof(IsGreaterThanValueToBooleanConverter),
            new PropertyMetadata(default(IComparable)));

        public IComparable MinValue
        {
            get => (IComparable)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IComparable) || MinValue == null)
            {
                return Binding.DoNothing;
            }

            var comparable = value as IComparable;
            return comparable.CompareTo(comparable) > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}