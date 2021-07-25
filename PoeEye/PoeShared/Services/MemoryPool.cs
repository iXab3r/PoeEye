using System;
using System.Buffers;

namespace PoeShared.Services
{
    public sealed class MemoryPool : IMemoryPool
    {
        public static IMemoryPool Shared => MemoryPoolSupplier.Value;

        private static readonly Lazy<IMemoryPool> MemoryPoolSupplier = new Lazy<IMemoryPool>(() => new MemoryPool());

        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(3840*2160*4 + 1, 32);

        private MemoryPool()
        {
        }
        
        public byte[] Rent(int minimumLength)
        {
            return arrayPool.Rent(minimumLength);
        }

        public void Return(byte[] array, bool clearArray = false)
        {
            // clearArray is extremely expensive, Return jumps from 1,408ns to 1,914,345ns on 4K RGBA image (~30mb)
            arrayPool.Return(array, clearArray);
        }
    }
}