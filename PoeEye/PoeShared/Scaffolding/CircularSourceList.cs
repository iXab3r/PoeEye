using DynamicData;

namespace PoeShared.Scaffolding;

public sealed class CircularSourceList<T> : DisposableReactiveObject, IObservableList<T>
{
    private readonly int capacity;
    private readonly ISourceListEx<T> innerList = new SourceListEx<T>();

    public CircularSourceList(int capacity)
    {
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

    public void Remove(T item)
    {
        innerList.Remove(item);
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
    public IObservable<int> CountChanged => innerList.CountChanged;
    public IEnumerable<T> Items => innerList.Items;
}