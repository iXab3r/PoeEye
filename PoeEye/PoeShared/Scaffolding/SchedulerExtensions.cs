using System.Reactive.Concurrency;

namespace PoeShared.Scaffolding;

public static class SchedulerExtensions
{
    public static void Run(this IScheduler scheduler, Action action)
    {
        Observable.Start(action, scheduler).Wait();
    }
}