using System.Reactive.Concurrency;

namespace PoeShared.Scaffolding;

public static class SchedulerExtensions
{
    [Obsolete("Left here for compatibility reasons - cancellationToken is no longer used")]
    public static void Run(this IScheduler scheduler, Action action, CancellationToken cancellationToken)
    {
        Run(scheduler, action);
    }
    
    public static void Run(this IScheduler scheduler, Action action)
    {
        Observable.Start(action, scheduler).Wait();
    }
}