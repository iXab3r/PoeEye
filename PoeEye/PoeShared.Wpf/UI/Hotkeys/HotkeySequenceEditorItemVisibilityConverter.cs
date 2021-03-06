﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceEditorItemVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2 || values[0] is not HotkeySequenceDelay delay || values[1] is not bool hideKeyPress)
            {
                return Visibility.Visible;
            }
            
            return !delay.IsKeypress || !hideKeyPress? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}