using System;
using System.Windows;
using System.Windows.Input;

namespace PoeShared.Scaffolding.WPF
{
    public class FocusHelper
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusHelper),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

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
                newValue = (bool) valueToSet.NewValue;
            }

            if (newValue == null)
            {
                return;
            }

            if (!newValue.Value)
            {
                return;
            }

            element.Loaded += ElementOnLoaded;
            element.IsVisibleChanged += ElementOnIsVisibleChanged;
            ApplyFocus(element);
        }

        private static void ApplyFocus(FrameworkElement element)
        {
            if (!element.IsLoaded || !element.IsVisible)
            {
                return;
            }
            if (element.IsFocused && element.IsKeyboardFocused)
            {
                return;
            }

            element.Focus();
            Keyboard.Focus(element);
        }
        private static void ElementOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }
            if (!element.IsVisible)
            {
                return;
            }
            ApplyFocus(element);
        }

        private static void ElementOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }
            if (!element.IsLoaded)
            {
                return;
            }
            element.Loaded -= ElementOnLoaded;
            ApplyFocus(element);
        }
    }
}