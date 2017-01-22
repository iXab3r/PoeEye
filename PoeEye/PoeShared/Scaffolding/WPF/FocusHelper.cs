using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoeShared.Scaffolding.WPF
{
    public class FocusHelper
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusHelper),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(
            DependencyObject associatedObject,
            DependencyPropertyChangedEventArgs valueToSet)
        {
            var element = associatedObject as FrameworkElement;
            if (element == null)
            {
                return;
            }

            bool? newValue = null;
            if (valueToSet.NewValue is string)
            {
                bool newBool;
                if (bool.TryParse(valueToSet.NewValue as string, out newBool))
                {
                    newValue = newBool;
                }
            }
            if (valueToSet.NewValue is bool)
            {
                newValue = (bool)valueToSet.NewValue;
            }

            if (newValue == null)
            {
                return;
            }

            if (newValue.Value)
            {
                element.Focus(); 
                Keyboard.Focus(element);
            }
        }
    }
}