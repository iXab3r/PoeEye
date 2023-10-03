using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Internals;

/// <summary>
/// Prototype of binder-based change detector
/// </summary>
internal sealed class ReactiveChangeDetector : DisposableReactiveObject
{
    private readonly IDictionary<ChangeTrackerKey, IChangeTracker> trackers = new Dictionary<ChangeTrackerKey, IChangeTracker>();
    private readonly Subject<object> whenChanged = new();

    public IObservable<object> WhenChanged => whenChanged;

    public TOut Track<TContext, TOut>(
        TContext context,
        Expression<Func<TContext, TOut>> selector) where TContext : class
    {
        var key = new ChangeTrackerKey(context, selector.ToString());

        ChangeTracker<TContext, TOut> tracker;
        if (trackers.TryGetValue(key, out var existingTracker))
        {
            tracker = (ChangeTracker<TContext, TOut>) existingTracker;
        }
        else
        {
            var newTracker = new ChangeTracker<TContext, TOut>(context, selector).AddTo(Anchors);
            newTracker.WhenChanged.Subscribe(whenChanged).AddTo(Anchors);
            
            trackers[key] = newTracker;
            tracker = newTracker;
        }

        return tracker.Value;
    }
}