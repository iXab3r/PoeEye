using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace PoeShared.Blazor.Scaffolding;

public sealed class ReactiveTrackerList : List<IObservable<string>>
{
    public ReactiveTrackerList(params IObservable<string>[] sources)
    {
        Add(sources);
    }
    

    public void Add(params IObservable<string>[] sources)
    {
        AddRange(sources);
    }
    
    public void Add<T>(IObservable<T> source)
    {
        base.Add(source.Select(x => x.ToString()));
    }
    
    public void Add<T, T1>(IObservable<T> source1, IObservable<T1> source2)
    {
        Add(Adapt(source1), Adapt(source2));
    }
    
    public void Add<T, T1, T2>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3)
    {
        Add(Adapt(source1), Adapt(source2), Adapt(source3));
    }
    
    public void Add<T, T1, T2, T3>(IObservable<T> source1, IObservable<T1> source2, IObservable<T2> source3, IObservable<T3> source4)
    {
        Add(Adapt(source1), Adapt(source2), Adapt(source3), Adapt(source4));
    }

    private static IObservable<string> Adapt<T>(IObservable<T> source)
    {
        return source.Select(x => x == null ? "null" : x.ToString());
    }
}