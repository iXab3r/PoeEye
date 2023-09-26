using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Services;

public sealed class ResourcePool<TKey, TResource> : DisposableReactiveObjectWithLogger
{
    private static readonly Binder<ResourcePool<TKey, TResource>> Binder = new();

    private readonly IClock clock;
    private readonly Func<TKey, TResource> resourceFactory;
    private readonly ConcurrentDictionary<TKey, ConcurrentQueue<ResourceContainer>> resourcesByKey = new();
    private readonly NamedLock resourcesLock = new($"resources of type {typeof(TResource)} and key {typeof(TKey)}");
    private TimeSpan lastCleanupTimestamp;

    static ResourcePool()
    {
        Binder.Bind(x => x.TimeToLive * 2).To(x => x.CleanupTimeout);
    }

    public ResourcePool(IClock clock, Func<TKey, TResource> resourceFactory)
    {
        this.clock = clock;
        this.resourceFactory = resourceFactory;
            
        Binder.Attach(this).AddTo(Anchors);
    }
        
    public DateTimeOffset LastCleanupTimestamp { get; private set; }
        
    public TimeSpan TimeToLive { get; set; }
        
    public TimeSpan CleanupTimeout { get; [UsedImplicitly] private set; }
        
    public IDisposable Rent(TKey key, out TResource instance)
    {
        using var @lock = resourcesLock.Enter();
            
        Log.Debug(() => $"Retrieving resource using key {key} from the pool");
            
        Cleanup();
            
        if (!TryGetFromPool(key, out var container))
        {
            Log.Debug(() => $"Not enough resources for the key {key}, creating a new one");
            var newResource = resourceFactory(key);
            container = new ResourceContainer(newResource);
            Log.Debug(() => $"Created new resource {newResource} and container {container}");
        }
        container.LastAccessTimestamp = clock.Elapsed;
            
        instance = container.Resource;
        return Disposable.Create(() =>
        {
            Log.Debug(() => $"Returning resource {container} to the pool");
            ReturnToPool(key, container);
            Log.Debug(() => $"Returned resource {container} to the pool");
        });
    }

    private void Cleanup()
    {
        if (TimeToLive <= TimeSpan.Zero)
        {
            return;
        }

        var elapsed = clock.Elapsed;
        var elapsedSinceCleanup = elapsed - lastCleanupTimestamp;
        if (elapsedSinceCleanup < CleanupTimeout)
        {
            return;
        }
            
        Log.Debug(() => $"Performing the cleanup, time elapsed: {elapsedSinceCleanup}");
        lastCleanupTimestamp = elapsed;
        LastCleanupTimestamp = clock.Now;

        foreach (var kvp in resourcesByKey)
        {
            while (kvp.Value.TryPeek(out var container) && elapsed - container.LastReleaseTimestamp > TimeToLive)
            {
                if (!kvp.Value.TryDequeue(out var containerToRemove))
                {
                    throw new InvalidStateException("Should've never happen - Peeked but failed to Dequeue an item");
                }

                Log.Debug(() => $"Removed container {containerToRemove} from the pool");

                if (containerToRemove.Resource is IDisposable disposableResource)
                {
                    Log.Debug(() => $"Disposing the resource {disposableResource} from container {containerToRemove}");
                    disposableResource.Dispose();
                }
            }
        }
            
        Log.Debug(() => $"Cleanup completed");
    }

    private bool TryGetFromPool(TKey key, out ResourceContainer instance)
    {
        if (resourcesByKey.TryGetValue(key, out var bag) && bag.TryDequeue(out var container))
        {
            instance = container;
            return true;
        }

        instance = default;
        return false;
    }

    private void ReturnToPool(TKey key, ResourceContainer instance)
    {
        instance.LastReleaseTimestamp = clock.Elapsed;
        var collection = resourcesByKey.GetOrAdd(key, new ConcurrentQueue<ResourceContainer>());
        collection.Enqueue(instance);
    }
        
    private sealed record ResourceContainer
    {
        public ResourceContainer(TResource Resource)
        {
            this.Resource = Resource;
        }

        public TimeSpan LastAccessTimestamp { get; set; }
            
        public TimeSpan LastReleaseTimestamp { get; set; }
        public TResource Resource { get; init; }

        public override string ToString()
        {
            return $"{nameof(LastAccessTimestamp)}: {LastAccessTimestamp}, {nameof(LastReleaseTimestamp)}: {LastReleaseTimestamp}, {nameof(Resource)}: {Resource}";
        }

        public void Deconstruct(out TResource Resource)
        {
            Resource = this.Resource;
        }
    }
}