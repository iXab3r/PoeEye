using Newtonsoft.Json;

namespace PoeShared.Services;

public interface IMemoryPool : IArrayPool<byte>
{
    PinnedMemoryBuffer RentPinnedBuffer(int minimumLength);
}