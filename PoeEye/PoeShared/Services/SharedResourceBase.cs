#define SHAREDRESOURCE_ENABLE_STACKTRACE_LOG

using System.Threading;

namespace PoeShared.Services;

public abstract class SharedResourceBase : DisposableReactiveObject, ISharedResource
{
    private static readonly long MaxRefCount = 64;

    private static long GlobalIdx;
    private readonly ReaderWriterLockSlim resourceGate = new(LockRecursionPolicy.SupportsRecursion);
    private readonly NamedLock refCountGate;
    public string ResourceId { get; }

    /// <summary>
    ///   RefCount is needed to share the same unmanaged Bitmap across multiple users
    ///   That allows to avoid extra memory allocations and collect memory much more quickly than usual GC cycle
    /// </summary>
    private long refCount = 1;

    protected SharedResourceBase()
    {
        ResourceId = $"Resource#{Interlocked.Increment(ref GlobalIdx)}";
        refCountGate = new NamedLock(ResourceId);
        Log = GetType().PrepareLogger().WithSuffix(ResourceId).WithSuffix(() => $"x{RefCount}").WithSuffix(ToString);
        Log.Debug(() => $"Resource is created");
        Disposable.Create(() => Log.Debug("Resource anchors are being disposed")).AddTo(Anchors);
    }

    public long RefCount
    {
        get
        {
            using (refCountGate.Enter())
            {
                return refCount;
            }
        }
    }

    public bool IsDisposed => Anchors.IsDisposed;

    protected IFluentLog Log { get; }

    public IDisposable RentReadLock()
    {
        Log.Debug(() => "Renting read lock");
        EnsureIsAlive();
        resourceGate.EnterReadLock();
        return Disposable.Create(() =>
        {
            Log.Debug(() => "Releasing read lock");
            resourceGate.ExitReadLock();
        });
    }

    public IDisposable RentWriteLock()
    {
        Log.Debug(() => "Renting write lock");
        EnsureIsAlive();
        resourceGate.EnterWriteLock();
        return Disposable.Create(() =>
        {
            Log.Debug(() => "Releasing write lock");
            resourceGate.ExitWriteLock();
        });
    }

    public bool TryRent()
    {
        using (refCountGate.Enter())
        {
            Log.Debug(() => "Resource is being rented");
            if (refCount <= 0)
            {
                Log.Debug(() => "Failed to rent - resource is already disposed");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                    WriteLog($"Failed to increment - already disposed");
#endif
                return false;
            }

            var usages = ++refCount;
            Log.Debug(() => "Resource is rented");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Incremented");
#endif
            if (usages > MaxRefCount)
            {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                    Log.Warn($"Resource has RefCount({usages}) greater than expected(max: {MaxRefCount}) on rental, leak @ {new StackTrace()}");
#else
                Log.Warn($"Resource has RefCount({usages}) greater than expected(max: {MaxRefCount}) on rental");
#endif
            }

            return true;
        }
    }

    public override void Dispose()
    {
        var usages = DecrementRefCount();
        if (usages > MaxRefCount)
        {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                Log.Warn($"Resource has RefCount({usages}) greater than expected(max: {MaxRefCount}) on disposal, leak @ {new StackTrace()}");
#else
            Log.Warn($"Resource has RefCount({usages}) greater than expected(max: {MaxRefCount}) on disposal");
#endif
        }

        if (usages > 0)
        {
            Log.Debug(() => $"Resource is still in use - keeping it");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Decrement, ignoring, still in use");
#endif
            return;
        }

        if (usages < 0)
        {
            throw new ObjectDisposedException($"Attempted to dispose already disposed(or scheduled for disposal) resource, usages: {usages}, IsDisposed: {Anchors.IsDisposed}");
        }

        if (resourceGate.IsWriteLockHeld)
        {
            throw new LockRecursionException($"Disposing resource under write-lock is not supported");
        }

        if (resourceGate.IsReadLockHeld)
        {
            throw new LockRecursionException($"Disposing resource under read-lock is not supported");
        }

        if (Anchors.IsDisposed)
        {
            throw new ObjectDisposedException($"Resource is already disposed: {this}");
        }

        Log.Debug(() => "Disposing resource");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
            WriteLog($"Disposing");
#endif
        base.Dispose();
        Log.Debug(() => "Resource disposed");
    }

    public void AddResource(IDisposable resource)
    {
        resource.AddTo(Anchors);
    }

    public void AddResource(Action disposeAction)
    {
        AddResource(Disposable.Create(disposeAction));
    }

    private long DecrementRefCount()
    {
        using (refCountGate.Enter())
        {
            var usages = --refCount;
            Log.Debug(() => "Resource is released");
            return usages;
        }
    }
        
    private void EnsureIsAlive()
    {
        var usages = RefCount;
        if (usages <= 0)
        {
            throw new ObjectDisposedException($"RefCount is {usages}, resource is not available");
        }
    }

    private string FormatPrefix()
    {
        return $"[{Thread.CurrentThread.ManagedThreadId,2}] [x{refCount}]";
    }

#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> log = new(new[] { $"[{Thread.CurrentThread.ManagedThreadId,2}] Created {new StackTrace()}" });
        private readonly int maxLogLength = 30;

        private void WriteLog(string message)
        {
            while (log.Count > maxLogLength && log.TryDequeue(out var _))
            {
            }

            log.Enqueue($"{FormatPrefix()} {message}, stack: {(new StackTrace(1))}");
        }
#endif
}