namespace PoeShared.Services
{
    public interface IMemoryPool
    {
        byte[] Rent(int minimumLength);

        void Return(byte[] array, bool clearArray = false);
    }
}