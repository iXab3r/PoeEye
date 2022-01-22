using System;
using System.Buffers;

namespace PoeShared.Services;

public sealed class MemoryPool : IMemoryPool
{
    public static IMemoryPool Shared => MemoryPoolSupplier.Value;

    /// <summary>
    /// The maximum length of an array instance that may be stored in the pool.
    /// </summary>
    private static readonly int MaxArraySize = 3840 * 2160 * 4 + 1;
        
    /// <summary>
    /// The maximum number of array instances that may be stored in each bucket in the pool. The pool groups arrays of similar lengths into buckets for faster access.
    /// </summary>
    private static readonly int MaxArrayPerBucket = 50;
        
    private static readonly Lazy<IMemoryPool> MemoryPoolSupplier = new Lazy<IMemoryPool>(() => new MemoryPool());

    private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(MaxArraySize, MaxArrayPerBucket);

    private MemoryPool()
    {
    }
        
    public byte[] Rent(int minimumLength)
    {
        return arrayPool.Rent(minimumLength);
    }

       
    public void Return(byte[] array)
    {
        // clearArray is extremely expensive, Return jumps from 1,408ns to 1,914,345ns on 4K RGBA image (~30mb)
        arrayPool.Return(array, clearArray: false);
    }
}