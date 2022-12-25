using System.Configuration;
using System.Threading;

namespace PoeShared.Scaffolding;

public static class TaskExtensions
{
    private static readonly IFluentLog Log = typeof(TaskExtensions).PrepareLogger();
    private static readonly int MinWaitHandleTimeoutInMs = 20;

    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        Sleep(cancellationToken, timeout, Log);
    }

    public static void Sleep(this CancellationToken cancellationToken, int millisecondsTimeout, IFluentLog log)
    {
        var sw = Stopwatch.StartNew();
        bool cancelled;
        if (millisecondsTimeout < MinWaitHandleTimeoutInMs)
        {
            log.Debug(() => $"Sleeping for {millisecondsTimeout}ms using context-switching");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (sw.ElapsedMilliseconds >= millisecondsTimeout)
                {
                    break;
                }
                Thread.Sleep(1);
            }
            cancelled = cancellationToken.IsCancellationRequested;
        }
        else
        {
            log.Debug(() => $"Sleeping for {millisecondsTimeout}ms");
            cancelled = cancellationToken.WaitHandle.WaitOne(millisecondsTimeout);
        }
        sw.Stop();
        if (cancelled)
        {
            log.Warn(() => $"Sleep for {millisecondsTimeout} was interrupted after {sw.ElapsedMilliseconds}ms");
        }
        else
        {
            if (sw.ElapsedMilliseconds > millisecondsTimeout * 2)
            {
                log.Warn(() => $"Sleep for {millisecondsTimeout}ms has completed after {sw.ElapsedMilliseconds}ms which is much longer than expected");
            }
            else
            {
                log.Debug(() => $"Sleep for {millisecondsTimeout}ms has completed after {sw.ElapsedMilliseconds}ms");
            }
        }
    }
    
    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout, IFluentLog log)
    {
        Sleep(cancellationToken, (int)timeout.TotalMilliseconds, log);
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