using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class FocusOnLeftClickBehavior : Behavior<UIElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseLeftButtonDown += AssociatedObjectOnMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseLeftButtonDown -= AssociatedObjectOnMouseLeftButtonDown;
    }

    private void AssociatedObjectOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!e.Handled && AssociatedObject.Focusable)
        {
            e.Handled = true;
            AssociatedObject.Focus();
        }
    }
}