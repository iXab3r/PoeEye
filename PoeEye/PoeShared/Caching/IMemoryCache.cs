namespace PoeShared.Caching;

public interface IMemoryCache<TKey, TValue>
{
    /// <summary>
    ///   Items is considered valid if it's lifetime is less than TTL, will be replaced with a new one otherwise on next request
    /// </summary>
    public TimeSpan TimeToLive { get; set; }

    /// <summary>
    ///   Time between cleanups. Cache is locked during cleanup period
    /// </summary>
    public TimeSpan CleanupPeriod { get; set; }
        
    /// <summary>
    ///   Items with lifetime > CleanupTimeToLive will be removed during Cleanup
    /// </summary>
    public TimeSpan CleanupTimeToLive { get; set; }
    
    public ICollection<TKey> Keys { get; }
    
    public ICollection<TValue> Values { get; }

    public bool ContainsKey(TKey key);
    
    public bool TryGetValue(TKey key, out TValue value);
        
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);
    
    public TValue GetOrUpdate(TKey key, Func<TKey, TValue, TValue> updateValue);
}