using System.Collections.Immutable;
using System.Reactive.Subjects;
using PoeShared.Services;

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

    IObservable<T[]> WhenAddedMany { get; }

    ImmutableHashSet<T> Items { get; }
}

public class ReactiveSet<T> : DisposableReactiveObject, IReadOnlyReactiveSet<T>
{
    private readonly ReplaySubject<T> whenAdded = new();
    private readonly NamedLock gate = new("ReactiveSetGate");

    public ReactiveSet() : this(EqualityComparer<T>.Default)
    {
    }

    public ReactiveSet(IEqualityComparer<T> comparer)
    {
        Items = ImmutableHashSet.Create(comparer);
    }

    public ImmutableHashSet<T> Items { get; private set; }

    public IObservable<T> WhenAdded => whenAdded;

    public IObservable<T[]> WhenAddedMany => Observable.Create<T[]>(observer =>
    {
        ImmutableHashSet<T> initialSet;
        using (gate.Enter()) // Lock to prevent add/subscribe race
        {
            initialSet = Items; 
            observer.OnNext(initialSet.ToArray());
        }
        
        return WhenAdded.Where(x => !initialSet.Contains(x)).Select(x => new[] {x}).Subscribe(observer);
    });

    public void Add(T element)
    {
        //fast-check
        if (Items.Contains(element))
        {
            return;
        }

        using (gate.Enter())
        {
            //double-check 
            if (Items.Contains(element))
            {
                return;
            }

            Items = Items.Add(element);
            whenAdded.OnNext(element);
        }
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