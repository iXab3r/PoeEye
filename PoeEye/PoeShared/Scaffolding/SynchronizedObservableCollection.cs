using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using DynamicData.Binding;
using PoeShared.Services;
using PropertyChanged;
using ReactiveUI;

namespace PoeShared.Scaffolding;

[DoNotNotify]
public sealed class SynchronizedObservableCollection<T> : DisposableReactiveObject, IObservableCollection<T>, IReadOnlyObservableCollection<T>
{
    private static long collectionIdx;

    private readonly ObservableCollectionExtended<T> collection = new();
    private readonly string collectionId = $"Collection<{typeof(T).Name}>#{Interlocked.Increment(ref collectionIdx)}";
    private readonly NamedLock syncRoot;

    public SynchronizedObservableCollection(IEnumerable<T> enumerable) : this()
    {
        collection.AddRange(enumerable);
    }

    public SynchronizedObservableCollection()
    {
        syncRoot = new NamedLock($"{collectionId} Lock");
        this.RaiseWhenSourceValue(x => x.Count, collection, x => x.Count).AddTo(Anchors);
        collection.CollectionChanged += OnCollectionChanged;
    }

    public NamedLock SyncRoot => syncRoot;

    public int Count
    {
        get
        {
            using var @lock = syncRoot.Enter();
            return collection.Count;
        }
    }

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        using var @lock = syncRoot.Enter();

        var clone = collection.ToImmutableList(); 
        return clone.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        using var @lock = syncRoot.Enter();
        var clone = collection.ToImmutableList(); 
        return ((IEnumerable)clone).GetEnumerator();
    }

    public void Add(T item)
    {
        using var @lock = syncRoot.Enter();
        collection.Add(item);
    }

    public void Clear()
    {
        using var @lock = syncRoot.Enter();
        collection.Clear();
    }

    public bool Contains(T item)
    {
        using var @lock = syncRoot.Enter();
        return collection.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        using var @lock = syncRoot.Enter();
        collection.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        using var @lock = syncRoot.Enter();
        return collection.Remove(item);
    }

    public int IndexOf(T item)
    {
        using var @lock = syncRoot.Enter();
        return collection.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        using var @lock = syncRoot.Enter();
        collection.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        using var @lock = syncRoot.Enter();
        collection.RemoveAt(index);
    }

    public T this[int index]
    {
        get
        {
            using var @lock = syncRoot.Enter();
            return collection[index];
        }
        set
        {
            using var @lock = syncRoot.Enter();
            collection[index] = value;
        }
    }

    public IDisposable SuspendCount()
    {
        using var @lock = syncRoot.Enter();
        return collection.SuspendCount();
    }

    public IDisposable SuspendNotifications()
    {
        using var @lock = syncRoot.Enter();
        return collection.SuspendNotifications();
    }

    public void Load(IEnumerable<T> items)
    {
        using var @lock = syncRoot.Enter();
        collection.Load(items);
    }

    public void Move(int oldIndex, int newIndex)
    {
        using var @lock = syncRoot.Enter();
        collection.Move(oldIndex, newIndex);
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var handler = CollectionChanged;
        handler?.Invoke(this, e);
    }
}