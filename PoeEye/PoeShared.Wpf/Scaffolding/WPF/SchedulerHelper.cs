using PoeShared.Modularity;
using System.Reactive.Concurrency;

namespace PoeShared.Scaffolding.WPF;

public static class SchedulerHelper
{
    public static IScheduler Current => DispatcherScheduler.Current;
}