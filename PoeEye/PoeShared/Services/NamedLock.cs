namespace PoeShared.Services;

public sealed class NamedLock
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(1); 
        
    private readonly Gate gate;
#if DEBUG
        private readonly ConcurrentDictionary<int, StackTrace> lockInfoByThreadId = new();
#endif
    public NamedLock(string name)
    {
        Log = typeof(NamedLock).PrepareLogger().WithSuffix(ToString);
        gate = new Gate(name);
    }
        
    private IFluentLog Log { get; }

    public IDisposable Enter()
    {
        Log.Debug(() => $"Acquiring lock");
        if (!Monitor.TryEnter(gate, DefaultTimeout))
        {
            Log.Warn($"Failed to acquire lock in {DefaultTimeout}ms");
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw new TimeoutException($"Failed to acquire lock {this}");
        }
        Log.Debug(() => $"Acquired lock");
            
#if DEBUG
            lockInfoByThreadId[Thread.CurrentThread.ManagedThreadId] = new StackTrace();
#endif
        return Disposable.Create(() =>
        {
            Log.Debug(() => $"Releasing lock");
            Monitor.Exit(gate);
            var isHolding = gate.IsEntered;
            Log.Debug(() => isHolding ? $"Released, but still holding the lock" : $"Released lock");
#if DEBUG
                if (isHolding || lockInfoByThreadId.TryRemove(Thread.CurrentThread.ManagedThreadId, out var _))
                {
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

    public override string ToString()
    {
        return $"Lock {gate.Name}, IsEntered: {gate.IsEntered}";
    }

    private sealed class Gate
    {
        public Gate(string name)
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