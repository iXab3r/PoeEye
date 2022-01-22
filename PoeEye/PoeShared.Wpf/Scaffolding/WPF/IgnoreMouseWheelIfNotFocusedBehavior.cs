using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class IgnoreMouseWheelIfNotFocusedBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
        "IsEnabled", typeof(bool), typeof(IgnoreMouseWheelIfNotFocusedBehavior), new PropertyMetadata(default(bool)));

    public bool IsEnabled
    {
        get { return (bool) GetValue(IsEnabledProperty); }
        set { SetValue(IsEnabledProperty, value); }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        base.OnDetaching();
    }

    private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!(sender is DependencyObject dependencyObject))
        {
            return;
        }

        if (!(dependencyObject.GetValue(UIElement.IsKeyboardFocusWithinProperty) is bool isFocused))
        {
            throw new InvalidOperationException($"Associated object {AssociatedObject} does not provide {UIElement.IsKeyboardFocusWithinProperty} value");
        }

        if (isFocused)
        {
            return;
        }
            
        MouseWheelHelper.HandlePreviewMouseWheel(dependencyObject, e, true);
    }
}