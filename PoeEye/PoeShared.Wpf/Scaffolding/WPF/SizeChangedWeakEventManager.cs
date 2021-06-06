using System.Windows;

namespace PoeShared.Scaffolding.WPF
{
    public class SizeChangedWeakEventManager : WeakEventManagerBase<SizeChangedWeakEventManager, FrameworkElement>
    {
        protected override void Start(FrameworkElement eventSource)
        {
            eventSource.SizeChanged += DeliverEvent;
        }

        protected override void Stop(FrameworkElement eventSource)
        {
            eventSource.SizeChanged -= DeliverEvent;
        }
    }
}
