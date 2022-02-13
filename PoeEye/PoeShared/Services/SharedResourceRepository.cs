using System.Collections.Concurrent;

namespace PoeShared.Services;

public sealed class SharedResourceRepository<TKey, TResource> where TResource : ISharedResource
{
    private readonly ConcurrentDictionary<TKey, TResource> cache = new();
    private readonly NamedLock gate;

    public SharedResourceRepository()
    {
        gate = new NamedLock(GetType().Name);
        Log = GetType().PrepareLogger();
    }
        
    private IFluentLog Log { get; }

    public int Count => cache.Count;

    public IObservable<TResource> ResolveOrAdd(TKey key, Func<TKey, TResource> resourceFactory)
    {
        return Observable.Using(() =>
        {
            var resource = GetOrAdd(key, resourceFactory);
            return resource;
        }, x => Observable.Return(x).Concat(Observable.Never<TResource>()));
    }

    public TResource GetOrAdd(TKey key, Func<TKey, TResource> resourceFactory)
    {
        return RentOrAddSession(key, resourceFactory);
    }

    public void Cleanup()
    {
        using var lockAnchor = gate.Enter();
        var removedItemCount = 0;
        foreach (var kvp in cache)
        {
            if (!kvp.Value.IsDisposed)
            {
                continue;
            }

            Log.Debug(() => $"Removing disposed item: {kvp}");
            if (!cache.TryRemove(kvp.Key, out var _))
            {
                Log.Debug(() => $"Successfully removed disposed item {kvp}");
                removedItemCount++;
            }
            else
            {
                Log.Debug(() => $"Someone else already removed disposed item {kvp}");
            }
        }

        if (removedItemCount > 0)
        {
            Log.Debug(() => $"Removed {removedItemCount} disposed item(s) from cache, total item count: {cache.Count}");
        }
    }

    private TResource RentOrAddSession(TKey key, Func<TKey, TResource> resourceFactory)
    {
        using var lockAnchor = gate.Enter();
        Cleanup();
        if (cache.TryGetValue(key, out var existingResource))
        {
            Log.WithSuffix(key).Debug(() => $"Resource already exists: {existingResource}, renting it");
            if (existingResource.TryRent())
            {
                return existingResource;
            }

            Log.WithSuffix(key).WithSuffix(existingResource).Debug(() => $"Failed to rent resource, removing it from cache");
            if (!cache.TryRemove(key, out var _))
            {
                Log.WithSuffix(key).WithSuffix(existingResource).Debug(() => $"Resource was already removed by someone else");
            }
            else
            {
                Log.WithSuffix(key).WithSuffix(existingResource).Debug(() => $"Removed resource");
            }

            return RentOrAddSession(key, resourceFactory);
        }
        Log.WithSuffix(key).Debug(() => $"Resource does not exist, creating a new one");
        var newValue = resourceFactory(key);
            
        Log.WithSuffix(key).Debug(() => $"Created new resource: {newValue}");
        if (!cache.TryAdd(key, newValue))
        {
            throw new ApplicationException($"Something went wrong - failed to add new resource {newValue} for key {key}");
        }

        return newValue;
    }
}