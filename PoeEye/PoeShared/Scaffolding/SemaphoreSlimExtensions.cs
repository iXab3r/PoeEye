namespace PoeShared.Scaffolding;

public static class SemaphoreSlimExtensions
{
    /// <summary>
    /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/> and returns a disposable
    /// object that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to rent.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is a disposable
    /// object that releases the semaphore when disposed.
    /// </returns>
    public static async Task<IDisposable> RentAsync(this SemaphoreSlim semaphoreSlim)
    {
        await semaphoreSlim.WaitAsync();
        return Disposable.Create(() => semaphoreSlim.Release());
    }

    /// <summary>
    /// Synchronously waits to enter the <see cref="SemaphoreSlim"/> and returns a disposable
    /// object that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to rent.</param>
    /// <returns>
    /// A disposable object that releases the semaphore when disposed.
    /// </returns>
    public static IDisposable Rent(this SemaphoreSlim semaphoreSlim)
    {
        semaphoreSlim.Wait();
        return Disposable.Create(() => semaphoreSlim.Release());
    }
}