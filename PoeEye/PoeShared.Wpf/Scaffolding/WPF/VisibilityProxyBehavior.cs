using System.Windows;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public sealed class VisibilityProxyBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
        "IsVisible", typeof(bool), typeof(VisibilityProxyBehavior), new PropertyMetadata(default(bool)));

    public bool IsVisible
    {
        get { return (bool) GetValue(IsVisibleProperty); }
        set { SetValue(IsVisibleProperty, value); }
    }
        
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.IsVisibleChanged += AssociatedObjectOnIsVisibleChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.IsVisibleChanged -= AssociatedObjectOnIsVisibleChanged;
        base.OnDetaching();
    }
        
    private void AssociatedObjectOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        IsVisible = AssociatedObject.IsVisible;
    }
}