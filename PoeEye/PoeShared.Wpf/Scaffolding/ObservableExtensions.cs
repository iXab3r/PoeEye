using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Threading;
using PoeShared.Common;
using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public static class ObservableExtensions
{
    public static IObservable<T> ObserveOnIfNeeded<T>(this IObservable<T> source, IScheduler scheduler)
    {
        return scheduler switch
        {
            DispatcherScheduler dispatcherScheduler => ObserveOnIfNeeded(source, dispatcherScheduler.Dispatcher),
            EnforcedThreadScheduler enforcedThreadScheduler => source.Select(x => enforcedThreadScheduler.IsOnSchedulerThread ? Observable.Return(x) : Observable.Return(x).ObserveOn(enforcedThreadScheduler)).Switch(),
            _ => throw new NotSupportedException($"Unsupported scheduler type: {scheduler}")
        };
    }
    
    public static IObservable<T> ObserveOnIfNeeded<T>(this IObservable<T> source, Dispatcher dispatcher)
    {
        return source
            .Select(x => dispatcher.CheckAccess() ? Observable.Return(x) : Observable.Return(x).ObserveOn(dispatcher)).Switch();
    }

    public static IObservable<T> Suspend<T>(
        this IObservable<T> source,
        IPauseController pauseController)
    {
        return source.Where(x => !pauseController.IsPaused);
    }
}