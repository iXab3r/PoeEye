using System.Threading;

namespace PoeShared.Scaffolding;

public static class TaskExtensions
{
    private static readonly IFluentLog Log = typeof(TaskExtensions).PrepareLogger();

    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        Sleep(cancellationToken, timeout, Log);
    }
    
    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout, IFluentLog log)
    {
        var sw = Stopwatch.StartNew();
        log.Debug(() => $"Sleeping for {timeout}");
        var cancelled = cancellationToken.WaitHandle.WaitOne(timeout);
        sw.Stop();
        if (cancelled)
        {
            log.Warn(() => $"Sleep for {timeout} was interrupted after {sw.Elapsed}");
        }
        else
        {
            log.Debug(() => $"Sleep for {timeout} has completed after {sw.Elapsed}");
        }
    }
    
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource())
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask != task)
            {
                throw new TimeoutException("The operation has timed out.");
            }

            timeoutCancellationTokenSource.Cancel();
            return await task;
        }
    }  
        
    public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
    {
        using ( var timeoutCancellationTokenSource = new CancellationTokenSource())
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask != task)
            {
                throw new TimeoutException("The operation has timed out.");
            }

            timeoutCancellationTokenSource.Cancel();
            await task;
        }
    }      
}