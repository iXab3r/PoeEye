#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

/// <summary>
/// Extension methods that provide RAII-style scopes for <see cref="SemaphoreSlim"/>.
/// </summary>
public static class SemaphoreSlimExtensions
{
    /// <summary>
    /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/> and returns a disposable
    /// scope that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to acquire.</param>
    /// <returns>
    /// A task that represents the asynchronous wait. The task result is a disposable scope
    /// that must be disposed (typically via <c>using</c>) to release the semaphore.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="semaphoreSlim"/> is <c>null</c>.</exception>
    public static Task<SemaphoreSlimAsyncScope> RentAsync(this SemaphoreSlim semaphoreSlim)
    {
        return SemaphoreSlimAsyncScope.Create(semaphoreSlim);
    }

    /// <summary>
    /// Asynchronously waits, up to the specified timeout, to enter the <see cref="SemaphoreSlim"/>
    /// and returns a disposable scope that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to acquire.</param>
    /// <param name="timeout">
    /// The maximum time to wait for the semaphore. A value of <see cref="Timeout.InfiniteTimeSpan"/>
    /// waits indefinitely.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous wait. The task result is a disposable scope
    /// that must be disposed (typically via <c>using</c>) to release the semaphore.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="semaphoreSlim"/> is <c>null</c>.</exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the semaphore could not be acquired within the specified <paramref name="timeout"/>.
    /// </exception>
    public static Task<SemaphoreSlimAsyncScope> RentAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        return SemaphoreSlimAsyncScope.Create(semaphoreSlim, timeout);
    }

    /// <summary>
    /// Synchronously waits to enter the <see cref="SemaphoreSlim"/> and returns a disposable
    /// scope that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to acquire.</param>
    /// <returns>
    /// A disposable scope that must be disposed (typically via <c>using</c>) to release the semaphore.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="semaphoreSlim"/> is <c>null</c>.</exception>
    public static SemaphoreSlimScope Rent(this SemaphoreSlim semaphoreSlim)
    {
        return SemaphoreSlimScope.Create(semaphoreSlim);
    }

    /// <summary>
    /// Synchronously waits, up to the specified timeout, to enter the <see cref="SemaphoreSlim"/>
    /// and returns a disposable scope that releases the semaphore when disposed.
    /// </summary>
    /// <param name="semaphoreSlim">The semaphore to acquire.</param>
    /// <param name="timeout">
    /// The maximum time to wait for the semaphore. A value of <see cref="Timeout.InfiniteTimeSpan"/>
    /// waits indefinitely.
    /// </param>
    /// <returns>
    /// A disposable scope that must be disposed (typically via <c>using</c>) to release the semaphore.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="semaphoreSlim"/> is <c>null</c>.</exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the semaphore could not be acquired within the specified <paramref name="timeout"/>.
    /// </exception>
    public static SemaphoreSlimScope Rent(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        return SemaphoreSlimScope.Create(semaphoreSlim, timeout);
    }

    /// <summary>
    /// A synchronous RAII-style scope for <see cref="SemaphoreSlim"/>.
    /// Acquires the semaphore on creation and releases it on <see cref="Dispose"/>.
    /// </summary>
    public readonly struct SemaphoreSlimScope : IDisposable
    {
        private readonly SemaphoreSlim? semaphore;

        private SemaphoreSlimScope(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        /// <summary>
        /// Releases the semaphore if it was successfully acquired.
        /// </summary>
        public void Dispose()
        {
            semaphore?.Release();
        }

        /// <summary>
        /// Synchronously acquires the specified semaphore and returns a scope that
        /// will release it when disposed.
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire. Must not be <c>null</c>.</param>
        /// <returns>A scope that releases the semaphore when disposed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="semaphore"/> is <c>null</c>.</exception>
        public static SemaphoreSlimScope Create(SemaphoreSlim semaphore)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));
            semaphore.Wait();
            return new SemaphoreSlimScope(semaphore);
        }

        /// <summary>
        /// Synchronously acquires the specified semaphore, waiting up to the given timeout,
        /// and returns a scope that will release it when disposed.
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire. Must not be <c>null</c>.</param>
        /// <param name="timeout">
        /// The maximum time to wait for the semaphore. A value of <see cref="Timeout.InfiniteTimeSpan"/>
        /// waits indefinitely.
        /// </param>
        /// <returns>A scope that releases the semaphore when disposed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="semaphore"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">
        /// Thrown if the semaphore could not be acquired within the specified <paramref name="timeout"/>.
        /// </exception>
        public static SemaphoreSlimScope Create(SemaphoreSlim semaphore, TimeSpan timeout)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));

            if (!semaphore.Wait(timeout))
            {
                throw new TimeoutException($"Timed out while waiting for SemaphoreSlim after {timeout}.");
            }

            return new SemaphoreSlimScope(semaphore);
        }
    }

    /// <summary>
    /// An asynchronous RAII-style scope for <see cref="SemaphoreSlim"/>.
    /// Acquires the semaphore before being returned and releases it on <see cref="Dispose"/>.
    /// </summary>
    public readonly struct SemaphoreSlimAsyncScope : IDisposable
    {
        private readonly SemaphoreSlim? semaphore;

        private SemaphoreSlimAsyncScope(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        /// <summary>
        /// Releases the semaphore if it was successfully acquired.
        /// </summary>
        public void Dispose()
        {
            semaphore?.Release();
        }

        /// <summary>
        /// Asynchronously acquires the specified semaphore and returns a scope
        /// that will release it when disposed.
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire. Must not be <c>null</c>.</param>
        /// <returns>
        /// A task whose result is a scope that releases the semaphore when disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="semaphore"/> is <c>null</c>.</exception>
        public static async Task<SemaphoreSlimAsyncScope> Create(SemaphoreSlim semaphore)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new SemaphoreSlimAsyncScope(semaphore);
        }

        /// <summary>
        /// Asynchronously acquires the specified semaphore, waiting up to the given timeout,
        /// and returns a scope that will release it when disposed.
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire. Must not be <c>null</c>.</param>
        /// <param name="timeout">
        /// The maximum time to wait for the semaphore. A value of <see cref="Timeout.InfiniteTimeSpan"/>
        /// waits indefinitely.
        /// </param>
        /// <returns>
        /// A task whose result is a scope that releases the semaphore when disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="semaphore"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">
        /// Thrown if the semaphore could not be acquired within the specified <paramref name="timeout"/>.
        /// </exception>
        public static async Task<SemaphoreSlimAsyncScope> Create(SemaphoreSlim semaphore, TimeSpan timeout)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));

            if (!await semaphore.WaitAsync(timeout).ConfigureAwait(false))
            {
                throw new TimeoutException($"Timed out while waiting for SemaphoreSlim after {timeout}.");
            }

            return new SemaphoreSlimAsyncScope(semaphore);
        }
    }
}
