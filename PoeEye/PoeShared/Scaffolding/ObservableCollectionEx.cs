using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using DynamicData.Binding;
using PoeShared.Services;
using PropertyBinder;
using PropertyChanged;
using ReactiveUI;

namespace PoeShared.Scaffolding;

[DoNotNotify]
public sealed class ObservableCollectionEx<T> : DisposableReactiveObject, IObservableCollection<T>, IReadOnlyObservableCollection<T>
{
    private static long collectionIdx;

    private readonly ObservableCollectionExtended<T> collection = new();
    private readonly string collectionId = $"Collection<{typeof(T).Name}>#{Interlocked.Increment(ref collectionIdx)}";

    private static readonly Binder<ObservableCollectionEx<T>> Binder = new();

    static ObservableCollectionEx()
    {
        Binder.Bind(x => x.collection.Count).To(x => x.Count);
    }

    public ObservableCollectionEx(IEnumerable<T> enumerable) : this()
    {
        collection.AddRange(enumerable);
    }

    public ObservableCollectionEx()
    {
        SyncRoot = new NamedLock($"{collectionId} Lock");
        collection.CollectionChanged += OnCollectionChanged;
        Binder.Attach(this).AddTo(Anchors);
    }

    public NamedLock SyncRoot { get; }

    public int Count { get; private set; }

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        var clone = collection.ToImmutableList(); 
        return clone.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var clone = collection.ToImmutableList(); 
        return ((IEnumerable)clone).GetEnumerator();
    }

    public void Add(T item)
    {
        collection.Add(item);
    }

    public void Clear()
    {
        collection.Clear();
    }

    public bool Contains(T item)
    {
        return collection.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        collection.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return collection.Remove(item);
    }

    public int IndexOf(T item)
    {
        return collection.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        collection.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        collection.RemoveAt(index);
    }

    public T this[int index]
    {
        get
        {
            return collection[index];
        }
        set
        {
            collection[index] = value;
        }
    }

    public IDisposable SuspendCount()
    {
        return collection.SuspendCount();
    }

    public IDisposable SuspendNotifications()
    {
        return collection.SuspendNotifications();
    }

    public void Load(IEnumerable<T> items)
    {
        collection.Load(items);
    }

    public void Move(int oldIndex, int newIndex)
    {
        collection.Move(oldIndex, newIndex);
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var handler = CollectionChanged;
        handler?.Invoke(this, e);
    }
}