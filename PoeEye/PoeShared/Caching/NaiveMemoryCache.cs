using JetBrains.Annotations;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Caching;

internal sealed class NaiveMemoryCache<TKey, TValue> : DisposableReactiveObject, IMemoryCache<TKey, TValue>
{
    private static readonly Binder<NaiveMemoryCache<TKey, TValue>> Binder = new();
    private static readonly IFluentLog Log = typeof(NaiveMemoryCache<TKey, TValue>).PrepareLogger();

    static NaiveMemoryCache()
    {
        Binder.Bind(x => x.TimeToLive.TotalMilliseconds).To(x => x.TimeToLiveInMilliseconds);
    }

    private readonly IClock clock;
    private readonly ConcurrentDictionary<TKey, Item> cache = new();
    private readonly ConcurrentDictionary<TKey, KeySemaphore> locksByKey = new();
    private DateTime lastCleanup;

    public NaiveMemoryCache(IClock clock)
    {
        this.clock = clock;
        Binder.Attach(this).AddTo(Anchors);
    }

    public TimeSpan TimeToLive { get; set; }

    public TimeSpan CleanupPeriod { get; set; }
        
    public TimeSpan CleanupTimeToLive { get; set; }

    public ICollection<TKey> Keys => cache.Keys;

    public ICollection<TValue> Values
    {
        get
        {
            var result = new List<TValue>();
            foreach (var key in Keys)
            {
                if (TryGetValue(key, out var value))
                {
                    result.Add(value);
                }
            }
            return result;
        }
    }

    private double TimeToLiveInMilliseconds { get; [UsedImplicitly] set; }

    public bool ContainsKey(TKey key)
    {
        return cache.ContainsKey(key);
    }
    
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (cache.TryGetValue(key, out var cacheEntry) && cacheEntry.ElapsedMilliseconds < TimeToLiveInMilliseconds)
        {
            value = cacheEntry.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryRemove(TKey key, out TValue removedItem)
    {
        if (cache.TryRemove(key, out var cacheEntry))
        {
            cacheEntry.Dispose();
            removedItem = cacheEntry.Value;
            return true;
        }

        removedItem = default;
        return false;
    }
    
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        return GetOrUpdate(key, (key, _) => valueFactory(key));
    }

    public TValue GetOrUpdate(TKey key, Func<TKey, TValue, TValue> updateValue)
    {
        var now = clock.Now;
        if (CleanupPeriod > TimeSpan.Zero && CleanupTimeToLive > TimeSpan.Zero && now - lastCleanup > CleanupPeriod)
        {
            lock (cache)
            {
                if (now - lastCleanup > CleanupPeriod)
                {
                    lastCleanup = now;
                    var itemsToRemove = cache.Where(x => x.Value.ElapsedMilliseconds > CleanupTimeToLive.TotalMilliseconds);
                    foreach (var keyValuePair in itemsToRemove)
                    {
                        TryRemove(keyValuePair.Key, out _);
                    }
                }
            }
        }
            
        if (TryGetValue(key, out var cacheEntry))
        {
            return cacheEntry;
        } 

        var keyLock = locksByKey.GetOrAdd(key, k => new KeySemaphore(k));
        keyLock.Wait();
            
        try
        {
            if (TryGetValue(key, out cacheEntry))
            {
                // someone has already updated the item before we took ownership over keyLock
                return cacheEntry;
            }

            var newValue = updateValue(key, TryRemove(key, out var removedItem) ? removedItem : default);
            if (newValue == null)
            {
                return default;
            }
            var newItem = new Item(newValue);
            cache[key] = newItem;
            return newItem.Value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    private sealed record Item : IDisposable
    {
        private readonly Stopwatch sw;

        public Item(TValue value)
        {
            sw = Stopwatch.StartNew();
            Value = value;
        }
            
        public TValue Value { get; }

        public long ElapsedMilliseconds => sw.ElapsedMilliseconds;

        public void Dispose()
        {
            if (Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private sealed record KeySemaphore
    {
        private static long GlobalId;

        private readonly TKey key;
        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

        private readonly long id = Interlocked.Increment(ref GlobalId);

        public KeySemaphore(TKey key)
        {
            this.key = key;
        }

        public void Wait()
        {
            semaphoreSlim.Wait();
        }
            
        public void Release()
        {
            semaphoreSlim.Release();
        }

        public override string ToString()
        {
            return $"Id: {id} Key: {key}";
        }
    }
}