using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public sealed class HierarchicalSourceCache<TObject, TKey> : DisposableReactiveObject, IHierarchicalSourceCache<TObject, TKey>
{
    private readonly ISourceCache<TObject, TKey> effectiveCache;
    private readonly ISourceCache<TObject, TKey> cache;

    public HierarchicalSourceCache(Func<TObject, TKey> keyExtractor)
    {
        effectiveCache = new SourceCache<TObject, TKey>(keyExtractor);
        cache = new SourceCache<TObject, TKey>(keyExtractor);
        
        var emptyCache = new SourceCache<TObject, TKey>(keyExtractor);
        this.WhenAnyValue(x => x.Parent)
            .Select(x => x as ISourceCache<TObject, TKey> ?? emptyCache)
            .Switch()
            .ForEachChange(change =>
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                    {
                        if (!cache.Lookup(change.Key).HasValue)
                        {
                            effectiveCache.AddOrUpdate(change.Current);
                        }
                    }
                        break;
                    case ChangeReason.Remove:
                    {
                        if (!cache.Lookup(change.Key).HasValue)
                        {
                            effectiveCache.RemoveKey(change.Key);
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(change), change, $"Unsupported reason: {change.Reason}, change: {change}");
                }
            })
            .Subscribe()
            .AddTo(Anchors);
        
        cache
            .Connect()
            .ForEachChange(change =>
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                    {
                        effectiveCache.AddOrUpdate(change.Current);
                    }
                        break;
                    case ChangeReason.Remove:
                    {
                        var parentCache = Parent;
                        var parentValue = parentCache?.Lookup(change.Key) ?? Optional<TObject>.None;
                        if (parentValue.HasValue)
                        {
                            effectiveCache.AddOrUpdate(parentValue.Value);
                        }
                        else
                        {
                            effectiveCache.RemoveKey(change.Key);
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(change), change, $"Unsupported reason: {change.Reason}, change: {change}");
                }
            })
            .Subscribe()
            .AddTo(Anchors);
    }

    public IHierarchicalSourceCache<TObject, TKey> Parent { get; set; }
    
    public IObservableCache<TObject, TKey> Effective => effectiveCache;

    public IObservable<IChangeSet<TObject, TKey>> Connect(Func<TObject, bool> predicate = null, bool suppressEmptyChangeSets = true)
    {
        return effectiveCache.Connect();
    }

    public IObservable<IChangeSet<TObject, TKey>> Preview(Func<TObject, bool> predicate = null)
    {
        return effectiveCache.Preview();
    }

    public IObservable<Change<TObject, TKey>> Watch(TKey key)
    {
        return effectiveCache.Watch(key);
    }

    public IObservable<int> CountChanged => effectiveCache.CountChanged;
    
    public Optional<TObject> Lookup(TKey key)
    {
        return effectiveCache.Lookup(key);
    }

    public int Count => effectiveCache.Count;

    public IEnumerable<TObject> Items => effectiveCache.Items;

    public IEnumerable<TKey> Keys => effectiveCache.Keys;

    public IEnumerable<KeyValuePair<TKey, TObject>> KeyValues => effectiveCache.KeyValues;

    public Func<TObject, TKey> KeySelector => effectiveCache.KeySelector;

    public void Edit(Action<ISourceUpdater<TObject, TKey>> updateAction)
    {
        cache.Edit(updateAction);
    }
}