using System.Windows;

namespace PoeShared.Scaffolding.WPF;

public class IsVisibleWeakEventManager : WeakEventManagerBase<IsVisibleWeakEventManager, FrameworkElement>
{
    protected override void Start(FrameworkElement eventSource)
    {
        eventSource.IsVisibleChanged += DeliverEvent;
    }

    protected override void Stop(FrameworkElement eventSource)
    {
        eventSource.IsVisibleChanged -= DeliverEvent;
    }

    private void DeliverEvent(object sender, DependencyPropertyChangedEventArgs e)
    {
        base.DeliverEvent(sender, new EventArgs<bool>((bool)e.NewValue));
    }
}