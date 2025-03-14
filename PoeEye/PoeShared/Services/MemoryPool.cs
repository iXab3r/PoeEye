using System.Buffers;
using Newtonsoft.Json;

namespace PoeShared.Services;

/// <summary>
/// Provides a resource pool that enables reusing instances of arrays.
/// </summary>
/// <remarks>
/// Internally uses ConfigurableArrayPool, as the expectation is that it will be used for large objects only(e.g. image canvases)
/// as in all other cases it would make more sense to use ArrayPool.Shared (TlsOverPerCoreLockedStacksArrayPool) as it is more performant
///
/// Notes: TlsOverPerCoreLockedStacksArrayPool max array size is only 1MB, meaning that it does not work for large arrays,
/// which is reasonable considering it uses threadlocal to store data
/// 
/// https://medium.com/@epeshk/the-big-performance-difference-between-arraypools-in-net-b25c9fc5e31d
/// </remarks>
public sealed class MemoryPool : IMemoryPool
{
    public static IMemoryPool Shared => MemoryPoolSupplier.Value;
    public static IMemoryPool Fake => FakeMemoryPoolSupplier.Value;

    /// <summary>
    /// The maximum length of an array instance that may be stored in the pool.
    /// </summary>
    private static readonly int MaxArraySize = 3840 * 2160 * 4 + 1; // 33MB
        
    /// <summary>
    /// The maximum number of array instances that may be stored in each bucket in the pool. The pool groups arrays of similar lengths into buckets for faster access.
    /// </summary>
    private static readonly int MaxArrayPerBucket = 50;
        
    private static readonly Lazy<IMemoryPool> MemoryPoolSupplier = new Lazy<IMemoryPool>(() => new MemoryPool());
    private static readonly Lazy<IMemoryPool> FakeMemoryPoolSupplier = new Lazy<IMemoryPool>(() => new FakeMemoryPool());

    private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(MaxArraySize, MaxArrayPerBucket);

    private long rentedArrays;
    
    private MemoryPool()
    {
    }

    public PinnedMemoryBuffer RentPinnedBuffer(int minimumLength)
    {
        return new PinnedMemoryBuffer(this, minimumLength);
    }

    public byte[] Rent(int minimumLength)
    {
        Interlocked.Increment(ref rentedArrays);
        return arrayPool.Rent(minimumLength);
    }

    public void Return(char[] array)
    {
        throw new NotImplementedException();
    }

    public void Return(byte[] array)
    {
        Interlocked.Decrement(ref rentedArrays);
        // clearArray is extremely expensive, Return jumps from 1,408ns to 1,914,345ns on 4K RGBA image (~30mb)
        arrayPool.Return(array, clearArray: false);
    }
}