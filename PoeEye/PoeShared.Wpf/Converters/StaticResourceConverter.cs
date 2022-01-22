using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

namespace PoeShared.Converters;

internal sealed class StaticResourceConverter : IValueConverter
{
    public object DefaultValue { get; set; }

    public bool ThrowWhenNotFound { get; set; } = true;
        
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }
        if (!(value is string resourceKey))
        {
            throw new ArgumentException($"Argument must be of type {typeof(string)}, got {value}");
        }
            
        return Application.Current.TryFindResource(resourceKey) ?? DefaultValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}