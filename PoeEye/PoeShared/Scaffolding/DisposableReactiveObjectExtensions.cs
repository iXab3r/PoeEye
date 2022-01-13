using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public static class DisposableReactiveObjectExtensions
    {
        public static IDisposable TrackAndDisposeResource<TSrc, TTarget>(this TSrc source, Expression<Func<TSrc, TTarget>> propertyAccessor) where TSrc : DisposableReactiveObject where TTarget : IDisposable
        {
            var anchors = new CompositeDisposable();

            TTarget latest = default;
            source.WhenAnyValue(propertyAccessor)
                .DisposePrevious()
                .Subscribe(x => latest = x)
                .AddTo(anchors);
            Disposable.Create(() =>
            {
                if (ReferenceEquals(latest, default))
                {
                    return;
                }
                latest.Dispose();
            }).AddTo(anchors);
            return anchors;
        }
    }
}