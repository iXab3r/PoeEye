using DynamicData;

namespace PoeShared.Scaffolding;

/// <summary>
/// Not thread safe ! There is a bug in ChangeAwareList(?) that leads to ArgumentOutOfRange in some cases
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class CircularSourceList<T> : DisposableReactiveObject, IObservableListEx<T>, ICollection<T>, ISourceList<T>
{
    private readonly int capacity;
    private readonly ISourceListEx<T> innerList = new SourceListEx<T>();

    public CircularSourceList(int capacity)
    {
        this.RaiseWhenSourceValue(x => x.Count, innerList, x => x.Count).AddTo(Anchors);
        this.capacity = capacity;
    }

    public void Add(T item)
    {
        innerList.Edit(list =>
        {
            var itemsToRemove = list.Count - capacity;
            if (itemsToRemove > 0)
            {
                list.RemoveRange(0, itemsToRemove);
            }
            list.Add(item);
        });
    }

    public void Clear()
    {
        innerList.Clear();
    }

    public bool Contains(T item)
    {
        return innerList.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        innerList.Items.ToArray().CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return innerList.Remove(item);
    }

    public IObservable<IChangeSet<T>> Connect(Func<T, bool> predicate = null)
    {
        return innerList.Connect(predicate);
    }

    public IObservable<IChangeSet<T>> Preview(Func<T, bool> predicate = null)
    {
        return innerList.Preview(predicate);
    }

    public int Count => innerList.Count;
    
    public bool IsReadOnly => false;
    
    public IObservable<int> CountChanged => innerList.CountChanged;
    
    public IEnumerable<T> Items => innerList.Items;
    
    public IReadOnlyObservableCollection<T> Collection => innerList.Collection;

    public IEnumerator<T> GetEnumerator()
    {
        return innerList.Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public void Edit(Action<IExtendedList<T>> updateAction)
    {
        innerList.Edit(updateAction);
    }
}