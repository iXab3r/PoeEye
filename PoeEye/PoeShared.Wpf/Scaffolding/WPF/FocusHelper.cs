using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class FocusHelper
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusHelper),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnIsFocusedPropertyChanged));

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

            element.SetCurrentValue(IsFocusedProperty, false);

            if (!ApplyFocus(element))
            {
            }

            element.IsVisibleChanged += ElementOnIsVisibleChanged;
        }
        
        private static bool ApplyFocus(FrameworkElement element)
        {
            element.Loaded -= ElementOnLoaded;

            if (!element.Focusable)
            {
                var targetElement = element
                    .FindVisualChildren<TextBox>()
                    .FirstOrDefault(x => x.Focusable) ?? element
                    .FindVisualChildren<Control>()
                    .FirstOrDefault(x => x.Focusable);
                if (targetElement == null)
                {
                    throw new ApplicationException($"Failed to find viable focusable element, src element: {element}");
                }

                return ApplyFocus(targetElement);
            }

            if (!element.IsLoaded || !element.IsVisible || !element.Focus())
            {
                element.Loaded += ElementOnLoaded;

                return false;
            }

            return element.IsFocused;
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

            ApplyFocus(element);
        }
    }
}