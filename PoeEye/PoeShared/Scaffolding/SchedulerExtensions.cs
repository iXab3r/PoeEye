using System.Reactive.Concurrency;
using System.Threading;

namespace PoeShared.Scaffolding;

public static class SchedulerExtensions
{
    public static IDisposable Schedule(this IScheduler scheduler, Action action, EventWaitHandle resetEvent)
    {
        return scheduler.Schedule(
            () =>
            {

                try
                {
                    action();
                }
                finally
                {
                    resetEvent.Set();
                }
            });
    }
}