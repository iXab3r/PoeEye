using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using DynamicData;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Scaffolding;

public sealed class ReactiveTrackerList : ConcurrentQueue<IObservable<string>>, ICanBeSealed
{
    private readonly AtomicFlag isSealed = new();

    public ReactiveTrackerList(params IObservable<string>[] sources)
    {
        Add(sources);
    }

    public void Add<T>(IObservable<T> source)
    {
        EnsureNotSealed();
        Enqueue(source.Select(x => x?.ToString()));
    }

    public void Add<T>(IObservable<IReadOnlyObservableCollection<T>> observableCollectionSource)
    {
        EnsureNotSealed();
        Enqueue(observableCollectionSource.Select(x => x != null ? x.ToObservableChangeSet() : new SourceList<T>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void Add<T, TKey>(IObservable<IHierarchicalSourceCache<T, TKey>> observableCacheSource)
    {
        EnsureNotSealed();
        Enqueue(observableCacheSource.Select(x => x != null ? x.ToObservableChangeSet() : new IntermediateCache<T, TKey>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void Add<T, TKey>(IObservable<IObservableCache<T, TKey>> observableCacheSource)
    {
        EnsureNotSealed();
        Enqueue(observableCacheSource.Select(x => x != null ? x.ToObservableChangeSet() : new IntermediateCache<T, TKey>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void Add<T, TKey>(IObservable<ISourceCache<T, TKey>> observableCacheSource)
    {
        AddCollection(observableCacheSource);
    }

    public void AddCollection<T, TKey>(IObservable<ISourceCache<T, TKey>> observableCacheSource)
    {
        EnsureNotSealed();
        Enqueue(observableCacheSource.Select(x => x != null ? x.ToObservableChangeSet() : new IntermediateCache<T, TKey>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void AddCollection<T>(IObservable<IObservableList<T>> observableListSource)
    {
        EnsureNotSealed();
        Enqueue(observableListSource.Select(x => x != null ? x.ToObservableChangeSet() : new SourceList<T>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void Add<T>(IObservable<IObservableList<T>> observableListSource)
    {
        AddCollection(observableListSource);
    }

    public void Add<T>(IObservable<ISourceList<T>> observableListSource)
    {
        EnsureNotSealed();
        Enqueue(observableListSource.Select(x => x != null ? x.ToObservableChangeSet() : new SourceList<T>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }

    public void Add<T>(IReadOnlyObservableCollection<T> observableCollection)
    {
        EnsureNotSealed();
        Enqueue(observableCollection.Connect().Select(x => x?.ToString()));
    }

    public void Add<T, TRet>(T source, Expression<Func<T, TRet>> expression) where T : INotifyPropertyChanged
    {
        Add(source.WhenAnyValue(expression));
    }

    public void Add(params IObservable<string>[] sources)
    {
        EnsureNotSealed();
        foreach (var src in sources)
        {
            Add<string>(src);
        }
    }

    public void Add<TOut>(IObservableList<TOut> observableList)
    {
        EnsureNotSealed();
        Add(observableList.Connect());
    }
    
    public void Add<T, TKey>(IObservableCache<T, TKey> observableCache)
    {
        EnsureNotSealed();
        Add(observableCache.Connect());
    }

    public void Add<TOut>(IObservable<IChangeSet<TOut>> changeSetObservable)
    {
        EnsureNotSealed();
        Add(changeSetObservable.Select(x => new {x.TotalChanges, AsString = x.ToString(), x.Replaced, x.Adds, x.Removes, x.Refreshes, x.Moves}));
    }

    public void Add<T, T1>(IObservable<T> source1, IObservable<T1> source2)
    {
        EnsureNotSealed();
        Add(Adapt(source1), Adapt(source2));
    }

    public void Add<T, T1, T2>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3)
    {
        EnsureNotSealed();
        Add(Adapt(source1), Adapt(source2), Adapt(source3));
    }

    public void Add<T, T1, T2, T3>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3, IObservable<T3> source4)
    {
        EnsureNotSealed();
        Add(Adapt(source1), Adapt(source2), Adapt(source3), Adapt(source4));
    }

    public ReactiveTrackerList With<T>(IObservable<T> source)
    {
        Add(source);
        return this;
    }

    public ReactiveTrackerList With<T>(IObservable<IReadOnlyObservableCollection<T>> observableCollectionSource)
    {
        Add(observableCollectionSource);
        return this;
    }

    public ReactiveTrackerList With<T, TKey>(IObservable<IHierarchicalSourceCache<T, TKey>> observableCacheSource)
    {
        Add(observableCacheSource);
        return this;
    }

    public ReactiveTrackerList With<T, TKey>(IObservable<IObservableCache<T, TKey>> observableCacheSource)
    {
        Add(observableCacheSource);
        return this;
    }

    public ReactiveTrackerList With<T, TKey>(IObservable<ISourceCache<T, TKey>> observableCacheSource)
    {
        Add(observableCacheSource);
        return this;
    }

    public ReactiveTrackerList With<T>(IObservable<IObservableList<T>> observableListSource)
    {
        Add(observableListSource);
        return this;
    }

    public ReactiveTrackerList With<T>(IObservable<ISourceList<T>> observableListSource)
    {
        Add(observableListSource);
        return this;
    }

    public ReactiveTrackerList With<T>(IReadOnlyObservableCollection<T> observableCollection)
    {
        Add(observableCollection);
        return this;
    }

    public ReactiveTrackerList With(params IObservable<string>[] sources)
    {
        Add(sources);
        return this;
    }

    public ReactiveTrackerList With<TOut>(IObservableList<TOut> observableList)
    {
        Add(observableList);
        return this;
    }
    
    public ReactiveTrackerList With<T, TKey>(IObservableCache<T, TKey> observableCache)
    {
        Add(observableCache);
        return this;
    }

    public ReactiveTrackerList WithCollection<T>(IObservable<IObservableList<T>> observableListSource)
    {
        AddCollection(observableListSource);
        return this;
    }
    
    public ReactiveTrackerList WithCollection<T, TKey>(IObservable<ISourceCache<T, TKey>> observableCacheSource)
    {
        AddCollection(observableCacheSource);
        return this;
    }
    
    public ReactiveTrackerList WithCollection<T, TKey>(IObservableCache<T, TKey> observableCache)
    {
        Add(observableCache);
        return this;
    }
    
    public ReactiveTrackerList WithCollection<T>(IObservableList<T> observableList)
    {
        Add(observableList);
        return this;
    }

    public ReactiveTrackerList With<TOut>(IObservable<IChangeSet<TOut>> changeSetObservable)
    {
        Add(changeSetObservable);
        return this;
    }

    public ReactiveTrackerList With<T, T1>(IObservable<T> source1, IObservable<T1> source2)
    {
        Add(source1, source2);
        return this;
    }

    public ReactiveTrackerList With<T, T1, T2>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3)
    {
        Add(source1, source2, source3);
        return this;
    }

    public ReactiveTrackerList With<T, T1, T2, T3>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3, IObservable<T3> source4)
    {
        Add(source1, source2, source3, source4);
        return this;
    }

    private static IObservable<string> Adapt<T>(IObservable<T> source)
    {
        return source.Select(x => x == null ? "null" : x.ToString());
    }

    public void Seal()
    {
        if (!isSealed.Set())
        {
        }
    }

    public bool IsSealed => isSealed.IsSet;

    private void EnsureNotSealed()
    {
        if (isSealed.IsSet)
        {
            throw new InvalidOperationException("List is already sealed - cannot add anything to it");
        }
    }
}