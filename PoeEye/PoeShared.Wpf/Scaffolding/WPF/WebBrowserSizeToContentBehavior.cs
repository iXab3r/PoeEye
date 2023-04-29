using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Xaml.Behaviors;

namespace PoeShared.Scaffolding.WPF;

public sealed class WebBrowserSizeToContentBehavior : Behavior<WebBrowser>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        
        AssociatedObject.Navigating += AssociatedObjectOnNavigating;
        AssociatedObject.LoadCompleted += AssociatedObjectOnLoadCompleted;
    }

    public int LineHeight { get; } = 20;

    private void AssociatedObjectOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        AssociatedObject.Width = ((dynamic)AssociatedObject.Document).body.scrollWidth;
        AssociatedObject.Height = ((dynamic)AssociatedObject.Document).body.scrollHeight;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Navigating -= AssociatedObjectOnNavigating;
        AssociatedObject.LoadCompleted -= AssociatedObjectOnLoadCompleted;
    }

    private void AssociatedObjectOnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        AssociatedObject.Width = 0;
        AssociatedObject.Height = 0;
    }
}