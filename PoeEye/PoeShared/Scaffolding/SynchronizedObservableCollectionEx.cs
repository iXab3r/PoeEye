using System.Collections.Specialized;
using DynamicData.Binding;
using PoeShared.Services;

namespace PoeShared.Scaffolding;

public sealed class SynchronizedObservableCollectionEx<T> : DisposableReactiveObject, IObservableCollection<T>, IReadOnlyObservableCollection<T>
{
    private readonly ObservableCollectionEx<T> collection = new();

    private readonly NamedLock gate = new NamedLock("SynchronizedObservableCollectionEx");

    public SynchronizedObservableCollectionEx()
    {
        collection.CollectionChanged += CollectionOnCollectionChanged;
    }

    private void CollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        using var @lock = gate.Enter();
        CollectionChanged?.Invoke(this, e);
    }

    public IEnumerator<T> GetEnumerator()
    {
        using var @lock = gate.Enter();
        return collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        using var @lock = gate.Enter();
        return ((IEnumerable) collection).GetEnumerator();
    }

    public void Add(T item)
    {
        using var @lock = gate.Enter();
        collection.Add(item);
    }

    public void Clear()
    {
        using var @lock = gate.Enter();
        collection.Clear();
    }

    public bool Contains(T item)
    {
        using var @lock = gate.Enter();
        return collection.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        using var @lock = gate.Enter();
        collection.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        using var @lock = gate.Enter();
        return collection.Remove(item);
    }

    public int Count
    {
        get
        {
            using var @lock = gate.Enter();
            return collection.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            using var @lock = gate.Enter();
            return collection.IsReadOnly;
        }
        
    }

    public int IndexOf(T item)
    {
        using var @lock = gate.Enter();
        return collection.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        using var @lock = gate.Enter();
        collection.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        using var @lock = gate.Enter();
        collection.RemoveAt(index);
    }

    public T this[int index]
    {
        get
        {
            using var @lock = gate.Enter();
            return collection[index];
        }
        set
        {
            using var @lock = gate.Enter();
            collection[index] = value;
        }
    }

    public IDisposable SuspendCount()
    {
        using var @lock = gate.Enter();
        return collection.SuspendCount();
    }

    public IDisposable SuspendNotifications()
    {
        using var @lock = gate.Enter();
        return collection.SuspendNotifications();
    }

    public void Load(IEnumerable<T> items)
    {
        using var @lock = gate.Enter();
        collection.Load(items);
    }

    public void Move(int oldIndex, int newIndex)
    {
        using var @lock = gate.Enter();
        collection.Move(oldIndex, newIndex);
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;
}