using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class SelectAllOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.GotKeyboardFocus += AssociatedObjectOnGotFocus;
    }

    private void AssociatedObjectOnGotFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(AssociatedObject.Text))
        {
            AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
        }
        else
        {
            AssociatedObject.SelectAll();
        }
    }

    private void AssociatedObjectOnTextChanged(object sender, TextChangedEventArgs e)
    {
        AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
        AssociatedObject.SelectAll();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
        AssociatedObject.GotKeyboardFocus -= AssociatedObjectOnGotFocus;
        base.OnDetaching();
    }
}