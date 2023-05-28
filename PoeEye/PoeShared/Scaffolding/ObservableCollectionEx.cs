using System.Collections.Immutable;
using System.Collections.Specialized;
using DynamicData.Binding;
using JetBrains.Annotations;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Scaffolding;

public sealed class ObservableCollectionEx<T> : DisposableReactiveObject, IObservableCollection<T>, IReadOnlyObservableCollection<T>
{
    private readonly ObservableCollectionExtended<T> collection = new();

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
        collection.CollectionChanged += OnCollectionChanged;
        Binder.Attach(this).AddTo(Anchors);
    }

    public int Count { get; [UsedImplicitly] private set; }

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
        get => collection[index];
        set => collection[index] = value;
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
        try
        {
            CollectionChanged?.Invoke(this, e);
        }
        catch (NotSupportedException)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw;
        }
    }
}