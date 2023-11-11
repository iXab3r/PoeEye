using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class EnableDragBehavior : Behavior<UIElement>
{
    private Point? popupMousePosition;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseDown += OnMouseDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseDown -= OnMouseDown;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var uiElement = sender as UIElement;

        var window = uiElement.FindVisualAncestor<Window>();
        if (window != null)
        {
            window.DragMove();
            return;
        }

        var popup = uiElement.FindLogicalAncestor<Popup>();
        if (popup != null)
        {
            popupMousePosition = e.GetPosition(popup);
            popup.Child.CaptureMouse();
            popup.MouseMove += PopupOnMouseMove;
            popup.MouseUp += PopupOnMouseUp;
        }
    }

    private void PopupOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Popup popup)
        {
            return;
        }

        popup.Child.ReleaseMouseCapture();
        popupMousePosition = null;
        popup.MouseMove -= PopupOnMouseMove;
        popup.MouseUp -= PopupOnMouseUp;
    }

    private void PopupOnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (sender is not Popup popup || popupMousePosition is not { } originalPoint)
        {
            return;
        }

        var currentPoint = e.GetPosition(popup);
        var offset = new Point(currentPoint.X - originalPoint.X, currentPoint.Y - originalPoint.Y);

        popup.HorizontalOffset += offset.X;
        popup.VerticalOffset += offset.Y;

        popupMousePosition = currentPoint;
    }
}