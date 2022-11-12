using System.Collections.Specialized;
using System.Reactive.Concurrency;
using DynamicData;

namespace PoeShared.Scaffolding;

/// <summary>
///  Slightly extended version of SourceList that materializes collection changes using specified scheduler or Immediate if not provided
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SourceListEx<T> : DisposableReactiveObject, ISourceListEx<T>
{
    private readonly ISourceList<T> sourceList;

    public SourceListEx(ISourceList<T> sourceList, IScheduler collectionScheduler = default)
    {
        this.sourceList = sourceList.AddTo(Anchors);
        var collectionSource = sourceList.Connect();
        if (collectionScheduler != null)
        {
            collectionSource.ObserveOn(collectionScheduler);
        }
        collectionSource.BindToCollection(out var collection).Subscribe().AddTo(Anchors);
        Collection = collection;
        this.RaiseWhenSourceValue(x => x.Count, collection, x => x.Count).AddTo(Anchors);
    }

    public SourceListEx(IObservable<IChangeSet<T>> source) : this(new SourceList<T>(source))
    {
    }

    public SourceListEx() : this(new SourceList<T>())
    {
    }
    
    public IReadOnlyObservableCollection<T> Collection { get; }
    
    public int Count => sourceList.Count; 

    public IObservable<int> CountChanged => sourceList.CountChanged;

    public IEnumerable<T> Items => sourceList.Items;

    public void Edit(Action<IExtendedList<T>> updateAction)
    {
        sourceList.Edit(updateAction);
    }

    public IObservable<IChangeSet<T>> Connect(Func<T, bool> predicate = null)
    {
        return sourceList.Connect(predicate);
    }

    public IObservable<IChangeSet<T>> Preview(Func<T, bool> predicate = null)
    {
        return sourceList.Preview(predicate);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => Collection.CollectionChanged += value;
        remove => Collection.CollectionChanged -= value;
    }
}