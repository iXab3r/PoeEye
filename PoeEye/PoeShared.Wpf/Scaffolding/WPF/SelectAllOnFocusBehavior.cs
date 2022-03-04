using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class SelectAllOnFocusBehavior : Behavior<TextBox>
{
    public static readonly DependencyProperty WaitForTextProperty = DependencyProperty.Register(
        "WaitForText", typeof(bool), typeof(SelectAllOnFocusBehavior), new PropertyMetadata(default(bool)));

    public bool WaitForText
    {
        get { return (bool)GetValue(WaitForTextProperty); }
        set { SetValue(WaitForTextProperty, value); }
    }
    
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.GotKeyboardFocus += AssociatedObjectOnGotFocus;
    }

    private void AssociatedObjectOnGotFocus(object sender, RoutedEventArgs e)
    {
        if (WaitForText && string.IsNullOrEmpty(AssociatedObject.Text))
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