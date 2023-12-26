using ReactiveUI;

namespace PoeShared.Services;

/// <summary>
/// Manages a shared resource of type <typeparamref name="T"/>, providing controlled access and ensuring efficient reuse.
/// This class is designed to handle scenarios where a resource is expensive to create and should be reused 
/// for as long as it is viable. The resource is created upon first request and subsequently "rented" out 
/// for use. The class ensures that the same resource instance is reused across multiple requests, maximizing efficiency.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SharedResource{T}"/> class is especially useful in situations where resource creation is costly,
/// such as network connections, database connections, or large data structures. It ensures that the overhead of 
/// creating such resources is incurred only once and the resource is reused until it is no longer valid or the 
/// <see cref="SharedResource{T}"/> itself is disposed.
/// </para>
/// <para>
/// The class also manages the lifecycle of the resource. The resource is expected to remain alive and usable 
/// until the <see cref="SharedResource{T}"/> class is disposed. Upon disposal, the <see cref="SharedResource{T}"/> 
/// class disposes of the managed resource as well, ensuring proper cleanup. If the resource disposes itself or 
/// becomes unusable (e.g., a network connection is lost), the class will automatically create a new instance of 
/// the resource upon the next request.
/// </para>
/// <para>
/// The class employs thread-safe mechanisms to ensure that the resource can be accessed concurrently from multiple 
/// threads without race conditions. This makes it suitable for use in multi-threaded environments.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the shared resource. Must be a subclass of <see cref="SharedResourceBase"/>.</typeparam>
public sealed class SharedResource<T> : DisposableReactiveObject where T : SharedResourceBase
{
    private static readonly IFluentLog Log = typeof(SharedResource<T>).PrepareLogger();
    private readonly Func<T> factory;
    private T instance;
    private readonly NamedLock factoryLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedResource{T}"/> class.
    /// </summary>
    /// <param name="factory">A factory function that creates new instances of the shared resource.</param>
    public SharedResource(Func<T> factory)
    {
        this.factory = factory;
        factoryLock = new NamedLock($"{GetType()}.factoryLock");
        Disposable.Create(() =>
        {
            var existing = Interlocked.Exchange(ref instance, null);
            if (existing != null)
            {
                Log.Debug($"Disposing instance of type {typeof(T)}: {existing}");
                existing.Dispose();
            }
        }).AddTo(Anchors);
    }

    /// <summary>
    /// Gets a value indicating whether the current instance of the shared resource is rented.
    /// </summary>
    public bool IsRented => Volatile.Read(ref instance)?.IsRented == true;

    /// <summary>
    /// Rents the current instance of the shared resource or creates a new one if necessary.
    /// </summary>
    /// <returns>The rented instance of the shared resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a newly created instance cannot be rented.</exception>
    public T RentOrCreate()
    {
        var currentInstance = Volatile.Read(ref instance);
        if (currentInstance != null && currentInstance.TryRent())
        {
            return currentInstance;
        }

        using var @lock = factoryLock.Enter();
        if (instance != null && instance.TryRent())
        {
            return instance;
        }

        Log.Debug($"{(instance == null ? $"Initializing new instance of type {typeof(T)}" : $"Re-initializing instance of type {typeof(T)}")}");
        var newInstance = factory();

        if (!newInstance.TryRent())
        {
            throw new InvalidOperationException($"Failed to rent newly-created instance of type {typeof(T)}: {newInstance}, refCount: {newInstance.RefCount}");
        }

        instance = newInstance;
        Log.Debug($"Created new instance of type {typeof(T)}: {instance}");

        return instance;
    }
}
