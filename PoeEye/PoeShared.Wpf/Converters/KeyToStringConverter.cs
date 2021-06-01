using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using PoeShared.UI.Hotkeys;

namespace PoeShared.Converters
{
    public sealed class KeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Key key)
            {
                return Binding.DoNothing;
            }

            var gesture = new HotkeyGesture(key);
            return HotkeyConverter.Instance.ConvertToString(gesture);
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
}