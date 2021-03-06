using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class EnableDragHelper
    {
        public static readonly DependencyProperty EnableDragProperty = DependencyProperty.RegisterAttached(
            "EnableDrag",
            typeof(bool),
            typeof(EnableDragHelper),
            new PropertyMetadata(default(bool), OnLoaded));

        private static void OnLoaded(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var uiElement = dependencyObject as UIElement;
            if (uiElement == null || dependencyPropertyChangedEventArgs.NewValue is bool == false)
            {
                return;
            }

            if ((bool) dependencyPropertyChangedEventArgs.NewValue)
            {
                uiElement.MouseMove += UIElementOnMouseMove;
            }
            else
            {
                uiElement.MouseMove -= UIElementOnMouseMove;
            }
        }

        private static void UIElementOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var uiElement = sender as UIElement;
            if (uiElement == null)
            {
                return;
            }

            if (mouseEventArgs.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }


            DependencyObject parent = uiElement;
            var avoidInfiniteLoop = 0;
            // Search up the visual tree to find the first parent window.
            while (parent is Window == false)
            {
                parent = VisualTreeHelper.GetParent(parent);
                avoidInfiniteLoop++;
                if (avoidInfiniteLoop == 1000)
                {
                    // Something is wrong - we could not find the parent window.
                    return;
                }
            }

            var window = parent as Window;
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    window.DragMove();
                }
            }));
        }

        public static void SetEnableDrag(DependencyObject element, bool value)
        {
            element.SetValue(EnableDragProperty, value);
        }

        public static bool GetEnableDrag(DependencyObject element)
        {
            return (bool) element.GetValue(EnableDragProperty);
        }
    }
}