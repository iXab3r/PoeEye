namespace PoeShared.Scaffolding;

public static class SemaphoreSlimExtensions
{
    public static async Task<IDisposable> Rent(this SemaphoreSlim semaphoreSlim)
    {
        await semaphoreSlim.WaitAsync();
        return Disposable.Create(() => semaphoreSlim.Release());
    }
}