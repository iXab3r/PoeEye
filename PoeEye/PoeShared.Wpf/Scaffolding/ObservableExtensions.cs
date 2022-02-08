using System;
using System.Reactive.Linq;
using System.Windows.Threading;

namespace PoeShared.Scaffolding;

public static class ObservableExtensions
{
    public static IObservable<T> ObserveOnIfNeeded<T>(this IObservable<T> source, Dispatcher dispatcher)
    {
        return source
            .Select(x => dispatcher.CheckAccess() ? Observable.Return(x) : Observable.Return(x).ObserveOn(dispatcher).Select(x => x)).Switch();
    } 
}