using System.Buffers;
using Newtonsoft.Json;

namespace PoeShared.Services;

/// <summary>
/// https://medium.com/@epeshk/the-big-performance-difference-between-arraypools-in-net-b25c9fc5e31d
/// </summary>
/// <remarks>
/// TlsOverPerCoreLockedStacksArrayPool max array size is only 1MB, meaning that it does not work for large arrays,
/// which is reasonable considering it uses threadlocal to store data
///
/// 1) Using "large" pool does not really work due to trimming
/// 2) It makes sense to implement byte/char pool which will repurpose same memory buffers
/// 3) Implement custom array pool for large objects to avoid trimming issue (maybe one of these? https://github.com/epeshk/arraypool-examples/blob/master/ArrayPoolTests/BoundedConcurrentQueue.cs)
/// 4) Need thorough testing before doing anything as there are way too many nuances, for now Shared pool should work for most cases
/// 5) Reading 30mb of JSON is way too expensive, this must be addressed! Very high chance that without that problem every other change will not be needed
/// </remarks>
/// <typeparam name="T"></typeparam>
public sealed class SharedArrayPool<T> : LazyReactiveObject<SharedArrayPool<T>>, IArrayPool<T>
{
    private static readonly IFluentLog Log = typeof(SharedArrayPool<T>).PrepareLogger();

    private readonly ArrayPool<T> sharedPool = ArrayPool<T>.Shared;
    
    /// <summary>
    /// The maximum length of an array instance that may be stored in the pool.
    /// </summary>
    private static readonly int MaxArraySize = 67_108_864; 
    private static readonly int LargeArraySize = 2000; 
        
    /// <summary>
    /// The maximum number of array instances that may be stored in each bucket in the pool. The pool groups arrays of similar lengths into buckets for faster access.
    /// </summary>
    private static readonly int MaxArrayPerBucket = 2;
    
    private readonly ArrayPool<T> largeObjectPool = ArrayPool<T>.Create(MaxArraySize, MaxArrayPerBucket); 
        
    public T[] Rent(int minimumLength)
    {
        if (minimumLength > LargeArraySize)
        {
            Log.Debug(() => $"Renting from large object pool {minimumLength}");
            var result = largeObjectPool.Rent(minimumLength);
            Log.Debug(() => $"Rented from large object {result.Length} (expected {minimumLength}");
            return result;
        }
        else
        {
            Log.Debug(() => $"Renting from pool {minimumLength}");
            var result = sharedPool.Rent(minimumLength);
            Log.Debug(() => $"Rented from pool {result.Length} (expected {minimumLength}");
            return result;
        }
       
    }

    public void Return(T[] array)
    {
        if (array.Length >= LargeArraySize)
        {
            Log.Debug(() => $"Returning to large object pool {array.Length}");
            sharedPool.Return(array, clearArray: false);
        }
        else
        {
            Log.Debug(() => $"Returning to pool {array.Length}");
            sharedPool.Return(array, clearArray: false);
        }
    }
}