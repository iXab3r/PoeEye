using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Scaffolding;

internal static class SchedulerExtensions
{
    public static void EnsureOnScheduler(this IScheduler scheduler)
    {
        if (!scheduler.IsOnScheduler())
        {
            throw new InvalidOperationException($"Operation must be completed on scheduler {scheduler}, but is on thread #{Environment.CurrentManagedThreadId}");
        }
    }
    
    public static bool IsOnScheduler(this IScheduler scheduler)
    {
        if (scheduler is IDispatcherScheduler dispatcherScheduler)
        {
            return dispatcherScheduler.CheckAccess();
        } 
        else
        {
            throw new ArgumentException($"Unsupported type of Scheduler: {scheduler}");
        }
    }   
    
    public static IObservable<T> ObserveOnIfNeeded<T>(this IObservable<T> source, DispatcherScheduler dispatcher)
    {
        return source
            .SelectMany(x => dispatcher.CheckAccess() ? Observable.Return(x) : Observable.Return(x).ObserveOn(dispatcher));
    }
}