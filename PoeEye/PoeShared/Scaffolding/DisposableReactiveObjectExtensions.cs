using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Subjects;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class DisposableReactiveObjectExtensions
{
    public static IDisposable TrackAndDisposeResource<TSrc, TTarget>(this TSrc source, Expression<Func<TSrc, TTarget>> propertyAccessor) where TSrc : DisposableReactiveObject where TTarget : IDisposable
    {
        var anchors = new CompositeDisposable();

        TTarget latest = default;
        source.WhenAnyValue(propertyAccessor)
            .DoWithPrevious(x => x?.Dispose())
            .Subscribe(x => latest = x)
            .AddTo(anchors);
        Disposable.Create(() => latest?.Dispose()).AddTo(anchors);
        return anchors;
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the specified 
    /// <see cref="IDisposableReactiveObject"/> is disposed. If the object is already disposed 
    /// when subscribing, it emits the value immediately. This method also ensures proper 
    /// cleanup of resources when the observer unsubscribes.
    /// </summary>
    /// <param name="source">The <see cref="IDisposableReactiveObject"/> to observe for disposal.</param>
    /// <returns>An observable sequence that emits a single Unit value when the object is disposed.</returns>
    public static IObservable<Unit> ListenWhenDisposed(this IDisposableReactiveObject source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Observable.Create<Unit>(observer =>
        {
            var subject = new ReplaySubject<Unit>(1);

            var notificationDisposable = Disposable.Create(HandleDisposed);
            source.Anchors.Add(notificationDisposable);

            return subject.Take(1).Subscribe(observer);

            void HandleDisposed()
            {
                subject.OnNext(Unit.Default);
                subject.OnCompleted();
            }
        });
    }
}