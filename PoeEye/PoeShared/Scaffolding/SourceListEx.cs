using System.Collections.Specialized;
using System.Reactive.Concurrency;
using DynamicData;
using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Scaffolding;

/// <summary>
///  Slightly extended version of SourceList that materializes collection changes using specified scheduler or Immediate if not provided
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SourceListEx<T> : DisposableReactiveObject, ISourceListEx<T>
{
    private static readonly Binder<SourceListEx<T>> Binder = new();
    private readonly ISourceList<T> sourceList;
    private readonly IReadOnlyObservableCollection<T> collection;

    static SourceListEx()
    {
    }

    public SourceListEx(ISourceList<T> sourceList, IScheduler scheduler = null)
    {
        this.sourceList = sourceList.AddTo(Anchors);
        sourceList.CountChanged.Subscribe(x => RaisePropertyChanged(nameof(Count))).AddTo(Anchors);
        var collectionSource = sourceList.Connect();
        if (scheduler != null)
        {
            collectionSource = collectionSource.ObserveOn(scheduler);
        }
        collectionSource.BindToCollection(out collection).Subscribe().AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
    }

    public SourceListEx(IObservable<IChangeSet<T>> source) : this(new SourceList<T>(source))
    {
    }

    public SourceListEx() : this(new SourceList<T>())
    {
    }

    public int Count => sourceList.Count;

    public IObservable<int> CountChanged => sourceList.CountChanged;

    public IEnumerable<T> Items => sourceList.Items;
    
    public IReadOnlyObservableCollection<T> Collection => collection;

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
        return collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //FIXME Dirty way of getting NON-THREAD-SAFE notifications. Bad.
    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => collection.CollectionChanged += value;
        remove => collection.CollectionChanged -= value;
    }

}