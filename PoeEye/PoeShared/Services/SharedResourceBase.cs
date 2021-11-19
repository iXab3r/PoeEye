#define SHAREDRESOURCE_ENABLE_STACKTRACE_LOG

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    public abstract class SharedResourceBase : DisposableReactiveObject
    {
        private static readonly int MaxRefCount = 64;

        private static long GlobalIdx;
        private readonly ReaderWriterLockSlim gate = new(LockRecursionPolicy.SupportsRecursion);
        private readonly string resourceId;

        /// <summary>
        ///   RefCount is needed to share the same unmanaged Bitmap across multiple users
        ///   That allows to avoid extra memory allocations and collect memory much more quickly than usual GC cycle
        /// </summary>
        private int refCount = 1;

        protected SharedResourceBase()
        {
            resourceId = $"Resource#{Interlocked.Increment(ref GlobalIdx)}";
            Log = GetType().PrepareLogger().WithSuffix(resourceId).WithSuffix(ToString);
        }

        public int RefCount => refCount;

        public bool IsDisposed => Anchors.IsDisposed;

        protected IFluentLog Log { get; }

        public IDisposable RentReadLock()
        {
            EnsureNotDisposed();
            gate.EnterReadLock();
            return Disposable.Create(() => gate.ExitReadLock());
        }

        public IDisposable RentWriteLock()
        {
            EnsureNotDisposed();
            gate.EnterWriteLock();
            return Disposable.Create(() => gate.ExitWriteLock());
        }

        public bool TryRent()
        {
            if (Anchors.IsDisposed)
            {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Failed to increment - already disposed");
#endif
                return false;
            }

            var usages = Interlocked.Increment(ref refCount);
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

            return usages > 0;
        }

        public override void Dispose()
        {
            Dispose("via IDisposable");
        }

        public void AddResource(IDisposable resource)
        {
            resource.AddTo(Anchors);
        }

        public void AddResource(Action disposeAction)
        {
            AddResource(Disposable.Create(disposeAction));
        }

        private void Dispose(string reason)
        {
            var usages = Interlocked.Decrement(ref refCount);
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
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Decrement, ignoring, still in use [{reason}]");
#endif
                return;
            }

            if (usages < 0)
            {
                throw new ObjectDisposedException($"Attempted to dispose already disposed(or scheduled for disposal) resource, usages: {usages}, IsDisposed: {Anchors.IsDisposed}");
            }

            if (gate.IsWriteLockHeld)
            {
                throw new LockRecursionException($"Disposing resource under write-lock is not supported");
            }

            if (gate.IsReadLockHeld)
            {
                throw new LockRecursionException($"Disposing resource under read-lock is not supported");
            }
            
            gate.EnterWriteLock();
            try
            {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Decrement, disposing [{reason}]");
#endif
                EnsureNotDisposed();
                base.Dispose();
            }
            finally
            {
                gate.ExitWriteLock();
            }
        }

        private void EnsureNotDisposed()
        {
            if (Anchors.IsDisposed)
            {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                throw new InvalidOperationException($"Anchors are already disposed:\n\t{log.DumpToString()}");
#else
                throw new InvalidOperationException($"Anchors for {this} are already disposed");
#endif
            }
        }

        private string FormatPrefix()
        {
            return $"[{Thread.CurrentThread.ManagedThreadId,2}] [x{refCount}]";
        }

#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
        private readonly ConcurrentQueue<string> log = new ConcurrentQueue<string>(new[] { $"[{Thread.CurrentThread.ManagedThreadId,2}] Created {new StackTrace()}" });
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
}