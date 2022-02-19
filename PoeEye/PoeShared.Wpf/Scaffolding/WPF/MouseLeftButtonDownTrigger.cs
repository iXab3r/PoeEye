using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class MouseLeftButtonDownTrigger : TriggerBase<FrameworkElement>
{
    public static readonly DependencyProperty ClickCountProperty = DependencyProperty.Register(
        "ClickCount", typeof(int), typeof(MouseLeftButtonDownTrigger), new PropertyMetadata(1));

    public int ClickCount
    {
        get { return (int)GetValue(ClickCountProperty); }
        set { SetValue(ClickCountProperty, value); }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseLeftButtonDown += AssociatedObjectOnMouseLeftButtonDown;
    }

    private void AssociatedObjectOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != ClickCount)
        {
            return;
        }

        e.Handled = true;
        base.InvokeActions(null);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.MouseLeftButtonDown -= AssociatedObjectOnMouseLeftButtonDown;
        base.OnDetaching();
    }
}