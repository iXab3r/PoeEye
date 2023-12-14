using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Blazor.Internals;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Scaffolding;

public static class ReactiveObjectExtensions
{
    public static IObservable<string> Listen<TContext, TOut>(this TContext context, Expression<Func<TContext, TOut>> selector) where TContext : class
    {
        return Observable.Create<string>(observer =>
        {
            var anchors = new CompositeDisposable();

            var detector = new ChangeTracker<TContext, TOut>(context, selector).AddTo(anchors);
            detector.WhenChanged.Select(x => x.ToString()).Subscribe(observer).AddTo(anchors);
            anchors.Add(() => { });
            return anchors;
        });
    }
}