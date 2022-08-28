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

    public static void Run(this IScheduler scheduler, Action action, CancellationToken cancellationToken)
    {
        var completionEvent = new ManualResetEventSlim();
        Exception inputException = null;
        scheduler.Schedule(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                action();
            }
            catch (Exception ex)
            {
                inputException = new AggregateException($"Inner operation has failed - {ex.Message}", ex);
            }
            finally
            {
                completionEvent.Set();
            }
        });
        completionEvent.Wait(cancellationToken);
        if (inputException != null)
        {
            throw inputException;
        }
    }
}