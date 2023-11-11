using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoeShared.Scaffolding.WPF;

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

public sealed class EnableDragHelper
{
    public static readonly DependencyProperty EnableDragProperty = DependencyProperty.RegisterAttached(
        "EnableDrag",
        typeof(bool),
        typeof(EnableDragHelper),
        new PropertyMetadata(default(bool), OnLoaded));


    private static void OnLoaded(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not UIElement uiElement)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            uiElement.MouseDown += UIElementOnMouseDown;
        }
        else
        {
            uiElement.MouseDown -= UIElementOnMouseDown;
        }
    }

    private static void UIElementOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement uiElement || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var window = uiElement.FindVisualAncestor<Window>();
        if (window != null)
        {
            window.DragMove();
            return;
        }
        
        var popup = uiElement.FindLogicalAncestor<Popup>();
        if (popup != null)
        {
            var mousePosition = e.MouseDevice.GetPosition(popup); // Get the mouse position relative to the screen
            popup.Tag = mousePosition;
            popup.Child.CaptureMouse();
            popup.MouseMove += PopupOnMouseMove;
            popup.MouseUp += PopupOnMouseUp;
        }
    }

    private static void PopupOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Popup popup)
        {
            return;
        }

        popup.Child.ReleaseMouseCapture();
        popup.Tag = null;
        popup.MouseMove -= PopupOnMouseMove;
        popup.MouseUp -= PopupOnMouseUp;
    }

    private static void PopupOnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not Popup {Tag: Point originalPoint} popup)
        {
            return;
        }

        var currentPoint = e.MouseDevice.GetPosition(popup); // Get the current mouse position relative to the screen
        var offset = new Point(currentPoint.X - originalPoint.X, currentPoint.Y - originalPoint.Y);

        // Adjust the popup's position
        popup.HorizontalOffset += offset.X;
        popup.VerticalOffset += offset.Y;

        // Update the starting point for the next move
        popup.Tag = currentPoint;
    }

    public static void SetEnableDrag(DependencyObject element, bool value)
    {
        element.SetValue(EnableDragProperty, value);
    }

    public static bool GetEnableDrag(DependencyObject element)
    {
        return (bool)element.GetValue(EnableDragProperty);
    }
}
