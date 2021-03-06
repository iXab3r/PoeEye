using System.Reactive.Concurrency;
using System.Windows.Threading;

namespace PoeShared.Wpf.Scaffolding.WPF
{
    public static class SchedulerHelper
    {
        public static IScheduler Current => DispatcherScheduler.Current;
    }
}