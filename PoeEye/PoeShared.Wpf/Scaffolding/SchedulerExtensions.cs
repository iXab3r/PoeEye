using System;
using System.Reactive.Concurrency;
using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public static class SchedulerExtensions
{
    public static void EnsureOnScheduler(this IScheduler scheduler)
    {
        if (!scheduler.IsOnScheduler())
        {
            throw new InvalidOperationException($"Operation must be completed on scheduler {scheduler}, but is on thread #{Environment.CurrentManagedThreadId}");
        }
    }
    
    public static void Invoke(this IScheduler scheduler, Action action)
    {
        if (scheduler is DispatcherScheduler dispatcherScheduler)
        {
            dispatcherScheduler.Dispatcher.Invoke(action);
        } 
        else
        {
            throw new ArgumentException($"Unsupported type of Scheduler: {scheduler}");
        }
    }

    public static T Invoke<T>(this IScheduler scheduler, Func<T> func)
    {
        if (scheduler is DispatcherScheduler dispatcherScheduler)
        {
            return dispatcherScheduler.Dispatcher.Invoke(func);
        } 
        else
        {
            throw new ArgumentException($"Unsupported type of Scheduler: {scheduler}");
        }
    }
    
    public static bool IsOnScheduler(this IScheduler scheduler)
    {
        if (scheduler is DispatcherScheduler dispatcherScheduler)
        {
            return dispatcherScheduler.Dispatcher.CheckAccess();
        } else if (scheduler is EnforcedThreadScheduler enforcedThreadScheduler)
        {
            return enforcedThreadScheduler.IsOnSchedulerThread;
        }
        else
        {
            throw new ArgumentException($"Unsupported type of Scheduler: {scheduler}");
        }
    }   
}