using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using PoeShared.UI;

namespace PoeShared.Converters;

public sealed class KeyToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            Key key => HotkeyConverter.Instance.ConvertToString( new HotkeyGesture(key)),
            MouseButton mouseButton => HotkeyConverter.Instance.ConvertToString( new HotkeyGesture(mouseButton)),
            MouseWheelAction mouseWheel => HotkeyConverter.Instance.ConvertToString( new HotkeyGesture(mouseWheel)),
            _ => Binding.DoNothing
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string keyString)
        {
            return Binding.DoNothing;
        }

        var gesture = HotkeyConverter.Instance.ConvertFromString(keyString);
        return gesture.Key;
    }
}