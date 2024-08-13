using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Scaffolding;

public sealed class ReactiveTrackerList : ConcurrentBag<IObservable<string>>, ICanBeSealed
{
    private readonly AtomicFlag isSealed = new();
    
    public ReactiveTrackerList(params IObservable<string>[] sources)
    {
        Add(sources);
    }
    
    public void Add<T>(IObservable<T> source)
    {
        EnsureNotSealed();
        base.Add(source.Select(x => x?.ToString()));
    }
    
    public void Add<T>(IObservable<IReadOnlyObservableCollection<T>> observableCollectionSource)
    {
        EnsureNotSealed();
        base.Add(observableCollectionSource.Select(x => x != null ? x.ToObservableChangeSet() : new SourceList<T>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }
    
    public void Add<T, TKey>(IObservable<IObservableCache<T, TKey>> observableCacheSource)
    {
        EnsureNotSealed();
        base.Add(observableCacheSource.Select(x => x != null ? x.ToObservableChangeSet() : new IntermediateCache<T, TKey>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }
    
    public void Add<T>(IObservable<IObservableList<T>> observableListSource)
    {
        EnsureNotSealed();
        base.Add(observableListSource.Select(x => x != null ? x.ToObservableChangeSet() : new SourceList<T>().ToObservableChangeSet()).Switch().Select(x => x?.ToString()));
    }
        
    public void Add<T>(IReadOnlyObservableCollection<T> observableCollection)
    {
        EnsureNotSealed();
        base.Add(observableCollection.Connect().Select(x => x?.ToString()));
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
    
    public void Add<TOut>(IObservable<IChangeSet<TOut>> changeSetObservable)
    {
        EnsureNotSealed();
        Add(changeSetObservable.Select(x =>  new{ x.TotalChanges, AsString = x.ToString(), x.Replaced, x.Adds, x.Removes, x.Refreshes }));
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

    private static IObservable<string> Adapt<T>(IObservable<T> source)
    {
        return source.Select(x => x == null ? "null" : x.ToString());
    }

    public void Seal()
    {
        if (!isSealed.Set())
        {
            return;
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