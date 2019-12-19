using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace PoeShared.Scaffolding
{
    public static class SchedulerExtensions
    {
        public static IDisposable Schedule(this IScheduler scheduler, Action action, ManualResetEvent resetEvent)
        {
            return scheduler.Schedule(
                () =>
                {
                    action();
                    resetEvent.Set();
                });
        }
    }
}