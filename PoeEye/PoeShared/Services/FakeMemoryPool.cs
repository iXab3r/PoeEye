namespace PoeShared.Services;

internal sealed class FakeMemoryPool : IMemoryPool
{
    public byte[] Rent(int minimumLength)
    {
        return new byte[minimumLength];
    }

    public void Return(byte[] array)
    {
        //
    }

    public PinnedMemoryBuffer RentPinnedBuffer(int minimumLength)
    {
        return new PinnedMemoryBuffer(this, minimumLength);
    }
}