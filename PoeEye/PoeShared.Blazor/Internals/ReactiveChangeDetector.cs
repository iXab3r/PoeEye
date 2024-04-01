using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Internals;

/// <summary>
/// Prototype of binder-based change detector
/// </summary>
internal sealed class ReactiveChangeDetector : DisposableReactiveObject
{
    private readonly IDictionary<ChangeTrackerKey, IChangeTracker> trackers = new Dictionary<ChangeTrackerKey, IChangeTracker>();
    private readonly IDictionary<object, IDisposable> observables = new Dictionary<object, IDisposable>();
    private readonly Subject<object> whenChanged = new();

    public IObservable<object> WhenChanged => whenChanged;

    public void TrackState<T>(IObservableList<T> source)
    {
        if (observables.TryGetValue(source, out var subscription))
        {
            return;
        }
        observables[source] = source.Connect().Subscribe(x =>
        {
            whenChanged.OnNext(x);
        }).AddTo(Anchors);
    }
    
    public void TrackState<T, TKey>(IObservableCache<T, TKey> source)
    {
        if (observables.TryGetValue(source, out var subscription))
        {
            return;
        }
        observables[source] = source.Connect().Subscribe(x =>
        {
            whenChanged.OnNext(x);
        }).AddTo(Anchors);
    }

    public TOut Track<TContext, TOut>(
        TContext context,
        Expression<Func<TContext, TOut>> selector) where TContext : class
    {
        var key = new ChangeTrackerKey(context, selector);

        ChangeTracker<TContext, TOut> tracker;
        if (trackers.TryGetValue(key, out var existingTracker))
        {
            tracker = (ChangeTracker<TContext, TOut>) existingTracker;
        }
        else
        {
            var newTracker = new ChangeTracker<TContext, TOut>(context, selector).AddTo(Anchors);
            newTracker.WhenChanged.Subscribe(x =>
            {
                whenChanged.OnNext(x);
            }).AddTo(Anchors);
            
            trackers[key] = newTracker;
            tracker = newTracker;
        }

        return tracker.Value;
    }
}