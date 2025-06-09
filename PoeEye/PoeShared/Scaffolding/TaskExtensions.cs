using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class TaskExtensions
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        Sleep(cancellationToken, timeout, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(this CancellationToken cancellationToken, double millisecondsTimeout)
    {
        Sleep(cancellationToken, millisecondsTimeout, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(TimeSpan timeout)
    {
        Sleep(timeout.TotalMilliseconds);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(double millisecondsTimeout)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, null);
    }
       
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(double millisecondsTimeout, IFluentLog log)
    {
        Sleep(CancellationToken.None, millisecondsTimeout, log);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(this CancellationToken cancellationToken, double millisecondsTimeout, IFluentLog log)
    {
        var sw = ValueStopwatch.StartNew();
        var isLogging = log?.IsDebugEnabled ?? false;

        var provider = SleepController.Instance.Provider;
        if (isLogging)
        {
            log.Debug($"Sleeping for {millisecondsTimeout}ms using combined wait, provider: {provider}");
        }
        
        provider.Sleep(millisecondsTimeout, cancellationToken);

        if (isLogging && cancellationToken.IsCancellationRequested)
        {
            log.Debug($"Sleep for {millisecondsTimeout} was interrupted after {sw.ElapsedMilliseconds}ms, provider: {{provider}}");
        }
    }
   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sleep(this CancellationToken cancellationToken, TimeSpan timeout, IFluentLog log)
    {
        Sleep(cancellationToken, timeout.TotalMilliseconds, log);
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
    
    /// <summary>
    /// Task will be awaited and exceptions will be forwarded to RxApp.DefaultExceptionHandler.
    /// </summary>
    public static async void AndForget(this ValueTask task, bool ignoreExceptions = false)
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
    
    /// <summary>
    /// Task will be awaited and exceptions will be forwarded to RxApp.DefaultExceptionHandler.
    /// </summary>
    public static async void AndForget<T>(this ValueTask<T> task, bool ignoreExceptions = false)
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