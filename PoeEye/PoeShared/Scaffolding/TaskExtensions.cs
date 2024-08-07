using System.Configuration;
using System.Threading;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class TaskExtensions
{
    public const int SleepWarningThresholdMs = 20;
    public const int SleepLowPrecisionThresholdMs = 15;

    private static readonly IFluentLog Log = typeof(TaskExtensions).PrepareLogger();
    private static readonly int MinWaitHandleTimeoutInMs = 50;

    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        Sleep(cancellationToken, timeout, null);
    }

    public static void Sleep(this CancellationToken cancellationToken, double millisecondsTimeout)
    {
        Sleep(cancellationToken, millisecondsTimeout, null);
    }

    public static void Sleep(TimeSpan timeout)
    {
        Sleep(timeout.TotalMilliseconds);
    }
    
    public static void Sleep(double millisecondsTimeout)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, null);
    }
       
    public static void Sleep(double millisecondsTimeout, IFluentLog log)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, log);
    }
    
    public static void Sleep(this CancellationToken cancellationToken, double millisecondsTimeout, IFluentLog log)
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

            var sleepDuration = (int)(millisecondsTimeout - SleepLowPrecisionThresholdMs);
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
            cancelled = cancellationToken.WaitHandle.WaitOne((int)millisecondsTimeout);
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