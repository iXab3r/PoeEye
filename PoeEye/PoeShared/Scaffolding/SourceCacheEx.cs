using System.Collections.Specialized;
using System.Reactive.Concurrency;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Scaffolding;

public sealed class SourceCacheEx<T, TKey> : DisposableReactiveObject, ISourceCacheEx<T, TKey>
{
    private static readonly Binder<SourceCacheEx<T, TKey>> Binder = new();
    private readonly ISourceCache<T, TKey> sourceCache;
    private readonly IReadOnlyObservableCollection<T> collection;

    static SourceCacheEx()
    {
    }


    public SourceCacheEx(Func<T, TKey> keySelector, IScheduler scheduler = null) : this(new SourceCache<T, TKey>(keySelector), scheduler)
    {
        
    }
    
    public SourceCacheEx(ISourceCache<T, TKey> sourceList, IScheduler scheduler = null)
    {
        this.sourceCache = sourceList.AddTo(Anchors);
        sourceList.CountChanged.Subscribe(x => Count = x).AddTo(Anchors);
        var collectionSource = sourceList.Connect();
        if (scheduler != null)
        {
            collectionSource = collectionSource.ObserveOn(scheduler);
        }
        collectionSource.BindToCollection(out collection).Subscribe().AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
    }

    public Optional<T> Lookup(TKey key)
    {
        return sourceCache.Lookup(key);
    }

    public IReadOnlyObservableCollection<T> Collection => collection;

    public int Count { get; [UsedImplicitly] private set; }

    public IObservable<IChangeSet<T, TKey>> Connect(Func<T, bool> predicate = null, bool suppressEmptyChangeSets = true)
    {
        return sourceCache.Connect(predicate, suppressEmptyChangeSets);
    }

    public IObservable<IChangeSet<T, TKey>> Preview(Func<T, bool> predicate = null)
    {
        return sourceCache.Preview(predicate);
    }

    public IObservable<Change<T, TKey>> Watch(TKey key)
    {
        return sourceCache.Watch(key);
    }

    public IObservable<int> CountChanged => sourceCache.CountChanged;

    public IEnumerable<T> Items => sourceCache.Items;

    public IEnumerable<TKey> Keys => sourceCache.Keys;

    public IEnumerable<KeyValuePair<TKey, T>> KeyValues => sourceCache.KeyValues;

    public Func<T, TKey> KeySelector => sourceCache.KeySelector;

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

    public void Edit(Action<ISourceUpdater<T, TKey>> updateAction)
    {
        sourceCache.Edit(updateAction);
    }

}