#define SHAREDRESOURCE_ENABLE_STACKTRACE_LOG

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    public abstract class SharedResourceBase : DisposableReactiveObject
    {
        private readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        ///   RefCount is needed to share the same unmanaged Bitmap across multiple users
        ///   That allows to avoid extra memory allocations and collect memory much more quickly than usual GC cycle
        /// </summary>
        private int refCount = 1;

        public int RefCount => refCount;

        public IDisposable RentReadLock()
        {
            gate.EnterReadLock();
            return Disposable.Create(() => gate.ExitReadLock());
        }

        public IDisposable RentWriteLock()
        {
            gate.EnterWriteLock();
            return Disposable.Create(() => gate.ExitWriteLock());
        }

        public bool TryRent()
        {
            try
            {
                gate.EnterReadLock();
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
                return usages > 0;
            }
            finally
            {
                gate.ExitReadLock();
            }
        }

        public override void Dispose()
        {
            try
            {
                gate.EnterUpgradeableReadLock();
                var usages = Interlocked.Decrement(ref refCount);
                if (usages > 0)
                {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                    WriteLog($"Decrement, ignoring, still in use");
#endif
                    return;
                }

#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                WriteLog($"Decrement, disposing");
#endif
                if (Anchors.IsDisposed)
                {
#if SHAREDRESOURCE_ENABLE_STACKTRACE_LOG && DEBUG
                    throw new InvalidOperationException($"Anchors are already disposed:\n\t{log.DumpToString()}");
#else
                    throw new InvalidOperationException($"Anchors for {this} are already disposed");
#endif
                }

                try
                {
                    gate.EnterWriteLock();
                    base.Dispose();
                }
                finally
                {
                    gate.ExitWriteLock();
                }
            }
            finally
            {
                gate.ExitUpgradeableReadLock();
            }
        }

        public void AddResource(IDisposable resource)
        {
            resource.AddTo(Anchors);
        }

        public void AddResource(Action disposeAction)
        {
            AddResource(Disposable.Create(disposeAction));
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