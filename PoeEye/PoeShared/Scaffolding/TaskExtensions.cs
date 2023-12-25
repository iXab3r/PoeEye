using System.Configuration;
using System.Threading;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class TaskExtensions
{
    public const int SleepWarningThresholdMs = 20;
    public const int SleepLowPrecisionThresholdMs = 12;

    private static readonly IFluentLog Log = typeof(TaskExtensions).PrepareLogger();
    private static readonly int MinWaitHandleTimeoutInMs = 50;

    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        Sleep(cancellationToken, timeout, null);
    }

    public static void Sleep(this CancellationToken cancellationToken, int millisecondsTimeout)
    {
        Sleep(cancellationToken, millisecondsTimeout, null);
    }

    public static void Sleep(TimeSpan timeout)
    {
        Sleep((int)timeout.TotalMilliseconds);
    }
    
    public static void Sleep(int millisecondsTimeout)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, null);
    }
    
       
    public static void Sleep(int millisecondsTimeout, IFluentLog log)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, log);
    }
    
    public static void Sleep(this CancellationToken cancellationToken, int millisecondsTimeout, IFluentLog log)
    {
        var sw = ValueStopwatch.StartNew();
        var isLogging = log?.IsDebugEnabled ?? false;
        bool cancelled;
        
        if (millisecondsTimeout < MinWaitHandleTimeoutInMs)
        {
            if (isLogging)
            {
                log.Debug($"Sleeping for {millisecondsTimeout}ms using combined wait");
            }

            var sleepDuration = millisecondsTimeout - SleepLowPrecisionThresholdMs;
            if (sleepDuration > 0)
            {
                Thread.Sleep(sleepDuration);
            }
            
            while (!cancellationToken.IsCancellationRequested && sw.ElapsedMilliseconds < millisecondsTimeout)
            {
                Thread.Yield();
            }

            cancelled = cancellationToken.IsCancellationRequested;
        }
        else
        {
            if (isLogging)
            {
                log.Debug($"Sleeping for {millisecondsTimeout}ms using wait handle");
            }
            cancelled = cancellationToken.WaitHandle.WaitOne(millisecondsTimeout);
        }
        
        if (cancelled)
        {
            if (isLogging)
            {
                log.Debug($"Sleep for {millisecondsTimeout} was interrupted after {sw.ElapsedMilliseconds}ms");
            }
        }
        else
        {
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            if (elapsedMilliseconds > SleepWarningThresholdMs && elapsedMilliseconds > millisecondsTimeout * 2)
            {
                if (isLogging)
                {
                    log.Debug($"Sleep for {millisecondsTimeout}ms has completed after {sw.ElapsedMilliseconds}ms which is much longer than expected");
                }
            }
            else
            {
                if (isLogging)
                {
                    log.Debug($"Sleep for {millisecondsTimeout}ms has completed after {sw.ElapsedMilliseconds}ms");
                }
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
    
    /// <summary>
    /// Task will be awaited and exceptions will be forwarded to RxApp.DefaultExceptionHandler.
    /// </summary>
    public static async void AndForget(this Task task, bool ignoreExceptions = false)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            if (!ignoreExceptions)
            {
                RxApp.DefaultExceptionHandler.OnNext(ex);
            }
        }
    }
}