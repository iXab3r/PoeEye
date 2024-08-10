using System.Collections.Immutable;
using System.Reactive.Subjects;

namespace PoeShared.Scaffolding;

public interface IReactiveList<T> : IReadOnlyReactiveList<T>
{
    void Add(T element);
}

public interface IReadOnlyReactiveList<T>
{
    IObservable<T> WhenAdded { get; }
    ImmutableArray<T> Items { get; }
}

public interface IReadOnlyReactiveSet<T>
{
    IObservable<T> WhenAdded { get; }
    ImmutableHashSet<T> Items { get; }
}

public class ReactiveSet<T> : DisposableReactiveObject, IReadOnlyReactiveSet<T>
{
    private readonly ReplaySubject<T> whenAdded = new();

    public ReactiveSet() : this(EqualityComparer<T>.Default)
    {
    }

    public ReactiveSet(IEqualityComparer<T> comparer)
    {
        Items = ImmutableHashSet.Create(comparer);
        whenAdded.Subscribe(x => Items = Items.Add(x)).AddTo(Anchors);
    }

    public ImmutableHashSet<T> Items { get; private set; }

    public IObservable<T> WhenAdded => whenAdded;

    public void Add(T element)
    {
        whenAdded.OnNext(element);
    }
}

public class ReactiveList<T> : DisposableReactiveObject, IReactiveList<T>
{
    private readonly ReplaySubject<T> whenAdded = new();

    public ReactiveList()
    {
        Items = ImmutableArray<T>.Empty;
        whenAdded.Subscribe(x => Items = Items.Add(x)).AddTo(Anchors);
    }

    public ImmutableArray<T> Items { get; private set; }

    public IObservable<T> WhenAdded => whenAdded;

    public void Add(T element)
    {
        whenAdded.OnNext(element);
    }
}