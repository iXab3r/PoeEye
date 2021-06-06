using System.Windows;

namespace PoeShared.Scaffolding.WPF
{
    public class LoadedWeakEventManager : WeakEventManagerBase<LoadedWeakEventManager, FrameworkElement>
    {
        protected override void Start(FrameworkElement eventSource)
        {
            eventSource.Loaded += DeliverEvent;
        }

        protected override void Stop(FrameworkElement eventSource)
        {
            eventSource.Loaded -= DeliverEvent;
        }
    }
}
