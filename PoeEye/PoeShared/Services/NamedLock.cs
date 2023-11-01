namespace PoeShared.Services;

/// <summary>
///   Class that implements locking with timeout and logging of locked threads
///   Logging is enabled only in debug configuration due to huge performance input
/// </summary>
public sealed class NamedLock
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(1); 
        
    private readonly GateHolder gate;
#if DEBUG
    // ReSharper disable once RedundantNameQualifier
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, StackTrace> lockInfoByThreadId = new();
#endif
    public NamedLock(string name)
    {
        Log = typeof(NamedLock).PrepareLogger().WithSuffix(ToString);
        gate = new GateHolder(name);
    }
#if DEBUG
    public bool EnableLogging { get; set; }
    
    public bool RecordStackTraces { get; set; }

#endif
    private IFluentLog Log { get; }

    public object Gate => gate;
    
    public IDisposable Enter(TimeSpan timeout)
    {
        
#if DEBUG
        var acquireStackTrace = RecordStackTraces ? new EnhancedStackTrace(new StackTrace()) : default(StackTrace);
        LogIfEnabled(() => $"Acquiring lock{PrepareStackLog(acquireStackTrace)}");
#endif
        
        if (!Monitor.TryEnter(gate, timeout))
        {
            Log.Warn($"Failed to acquire lock in {timeout}ms");
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw new TimeoutException($"Failed to acquire lock {this}");
        }
            
#if DEBUG
        lockInfoByThreadId[Thread.CurrentThread.ManagedThreadId] = acquireStackTrace;
        LogIfEnabled(() => $"Acquired lock");
#endif
        
        return Disposable.Create(() =>
        {
#if DEBUG
            var releaseStackTrace = RecordStackTraces ? new EnhancedStackTrace(new StackTrace()) : default(StackTrace);
            LogIfEnabled(() => $"Releasing lock{PrepareStackLog(releaseStackTrace)}");
#endif
            
            Monitor.Exit(gate);
            var isHolding = gate.IsEntered;
            LogIfEnabled(() => isHolding ? $"Released, but still holding the lock" : $"Released lock");

#if DEBUG
            if (isHolding)
            {
                LogIfEnabled(() => $"Released, but still holding the lock");
                return;
            }

            if (lockInfoByThreadId.TryRemove(Environment.CurrentManagedThreadId, out _))
            {
                LogIfEnabled(() => $"Released lock");
                return;
            }

            Log.Warn($"Failed to cleanup thread lock info for {new { Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId }}, threads holding lock: {lockInfoByThreadId.Keys.DumpToString()}");
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw new ApplicationException($"Failed to cleanup thread lock info, lock {this}");
#endif
        });
    }

    public bool IsEntered => gate.IsEntered;
    
    public IDisposable Enter()
    {
        return Enter(DefaultTimeout);
    }

    public override string ToString()
    {
        return $"Lock {gate.Name}, IsEntered: {gate.IsEntered}";
    }

    private string PrepareStackLog(StackTrace stackTrace)
    {
        if (stackTrace == null)
        {
            return string.Empty;
        }
        var allFrames = stackTrace.GetFrames() ?? Array.Empty<StackFrame>();
        var framesToShow = allFrames.Length > 30 ? allFrames.Length / 3 : 10;
        return $"@\n\t{allFrames.DumpToNamedTable("Frames", framesToShow)}";
    }

    private void LogIfEnabled(Func<string> messageSupplier)
    {
#if DEBUG
        if (!EnableLogging)
        {
            return;
        }

        Log.DebugIfDebug(messageSupplier);
#endif
    }

    private sealed class GateHolder
    {
        public GateHolder(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool IsEntered => Monitor.IsEntered(this);

        public override string ToString()
        {
            return $"Gate {Name}";
        }
    }
}