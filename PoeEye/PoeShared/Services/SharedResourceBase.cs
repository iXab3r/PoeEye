//#define SHAREDRESOURCE_ENABLE_STACKTRACE_LOG
//#define SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS

using System.Threading;

namespace PoeShared.Services;

public abstract class SharedResourceBase<T> : SharedResourceBase
{
    // ReSharper disable once StaticMemberInGenericType intended behavior
    private static long aliveResourcesCount;
    
    protected SharedResourceBase()
    {
        var beforeAliveCount = Interlocked.Increment(ref aliveResourcesCount);
        Log.Debug($"Created, alive items count: {beforeAliveCount}");

        Disposable.Create(() =>
        {
            var afterAliveCount = Interlocked.Decrement(ref aliveResourcesCount);
            Log.Debug($"Disposed, alive items count: {afterAliveCount}");
        }).AddTo(Anchors);
    }
}

public abstract class SharedResourceBase : DisposableReactiveObject, ISharedResource
{
    private static readonly long MaxRefCount = 1024;

    private readonly ReaderWriterLockSlim resourceGate = new(LockRecursionPolicy.SupportsRecursion);
    private readonly NamedLock refCountGate;
    private static long globalIdx;

    /// <summary>
    ///   RefCount is needed to share the same unmanaged Bitmap/Image/etc across multiple threads and control lifecycle
    ///   That allows to avoid extra memory allocations 
    /// </summary>
    private long refCount = 1;

    protected SharedResourceBase()
    {
        ResourceId = $"Resource#{Interlocked.Increment(ref globalIdx)}";
        refCountGate = new NamedLock(ResourceId);
        Log = GetType().PrepareLogger().WithSuffix(ToString);
        Log.Debug($"Resource of type {GetType()} is created");

        Disposable.Create(() =>
        {
            Log.Debug($"Resource of type {GetType()} has been disposed");
        }).AddTo(Anchors);
    }
    
    public string ResourceId { get; }

    public bool IsRented => RefCount > 1;

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
    
    public bool IsWriteLockHeld => Gate.IsWriteLockHeld;
    
    public bool IsReadLockHeld => Gate.IsReadLockHeld;

    public ReaderWriterLockSlim Gate => resourceGate;

    protected IFluentLog Log { get; }

    public IDisposable RentReadLock()
    {
        Log.Debug("Entering Read lock");
        EnsureIsAlive();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
        WriteLog($"Entering Read lock: {resourceGate}");
#endif
        resourceGate.EnterReadLock();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
        WriteLog($"Entered Read lock: {resourceGate}");
#endif
        return Disposable.Create(() =>
        {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
            WriteLog($"Exiting Read lock: {resourceGate}");
#endif
            Log.Debug("Exiting Read lock");
            resourceGate.ExitReadLock();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
            WriteLog($"Exited Read lock: {resourceGate}");
#endif
        });
    }

    public IDisposable RentWriteLock()
    {
        Log.Debug("Entering Write lock");
        EnsureIsAlive();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
        WriteLog($"Entering Write lock: {resourceGate}");
#endif
        resourceGate.EnterWriteLock();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
        WriteLog($"Entered Write lock: {resourceGate}");
#endif
        return Disposable.Create(() =>
        {
            Log.Debug("Exiting Write lock");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
            WriteLog($"Exiting Write lock: {resourceGate}");
#endif
            resourceGate.ExitWriteLock();
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && SHAREDRESOURCE_ENABLE_STACKTRACE_LOG_LOCKS && DEBUG
            WriteLog($"Exited Write lock: {resourceGate}");
#endif
        });
    }

    public bool TryRent()
    {
        using (refCountGate.Enter())
        {
            Log.Debug($"Resource is being rented, refCount: {refCount}");
            if (refCount <= 0)
            {
                Log.Debug("Failed to rent - resource is already disposed");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                    WriteLog($"Failed to increment - already disposed");
#endif
                return false;
            }

            if (!CanRent())
            {
                Log.Debug("Failed to rent - resource could not be rented");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Failed to increment - resource could not be rented");
#endif
                return false;
            }

            var usages = ++refCount;
            Log.Debug($"Resource is rented, refCount: {usages}");
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

    protected virtual bool CanRent()
    {
        return true;
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
            Log.Debug($"Resource is still in use - keeping it, refCount: {refCount}");
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

        Log.Debug("Disposing resource");
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
            WriteLog($"Disposing");
#endif
        base.Dispose();
        Log.Debug("Resource disposed");
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
            Log.Debug($"Resource is released, refCount: {usages}");
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