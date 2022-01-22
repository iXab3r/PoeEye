using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class IgnoreMouseClickBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty ChangedButtonProperty = DependencyProperty.Register(
        "ChangedButton", typeof(MouseButton), typeof(IgnoreMouseClickBehavior), new PropertyMetadata(default(MouseButton)));

    public static readonly DependencyProperty MinClickCountProperty = DependencyProperty.Register(
        "MinClickCount", typeof(int), typeof(IgnoreMouseClickBehavior), new PropertyMetadata(default(int)));

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
        "IsEnabled", typeof(bool), typeof(IgnoreMouseClickBehavior), new PropertyMetadata(default(bool)));

    public MouseButton ChangedButton
    {
        get { return (MouseButton)GetValue(ChangedButtonProperty); }
        set { SetValue(ChangedButtonProperty, value); }
    }

    public int MinClickCount
    {
        get { return (int)GetValue(MinClickCountProperty); }
        set { SetValue(MinClickCountProperty, value); }
    }

    public bool IsEnabled
    {
        get { return (bool)GetValue(IsEnabledProperty); }
        set { SetValue(IsEnabledProperty, value); }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObjectOnPreviewMouseLeftButtonDown;
    }

    private void AssociatedObjectOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }
            
        if (e.ChangedButton != ChangedButton)
        {
            return;
        }

        if (e.ClickCount < MinClickCount)
        {
            return;
        }

        e.Handled = true;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObjectOnPreviewMouseLeftButtonDown;
        base.OnDetaching();
    }
}