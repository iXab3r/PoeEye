#if DEBUG && false
#define OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
#endif

using DynamicData;
using DynamicData.Binding;

namespace PoeShared.Scaffolding;

public class LoggingChangeSetAdaptor<T> : DisposableReactiveObjectWithLogger, IChangeSetAdaptor<T>
{
    private readonly IObservableCollection<T> observableCollectionEx;
    private readonly ObservableCollectionAdaptor<T> observableCollectionAdaptor;

    private readonly int parentThread = Environment.CurrentManagedThreadId;
    private readonly string parentStackTraceInfo = new StackTrace().ToString();
    
    public LoggingChangeSetAdaptor(IObservableCollection<T> observableCollectionEx)
    {
        Log.WithSuffix($"<{typeof(T).Name}>").WithSuffix($"TID {parentThread}");
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Collection of type {typeof(T)} is created @ {parentStackTraceInfo}");
#endif
        this.observableCollectionEx = observableCollectionEx;
        observableCollectionAdaptor = new ObservableCollectionAdaptor<T>(observableCollectionEx, refreshThreshold: int.MaxValue);
    }
    
    public void Adapt(IChangeSet<T> changeSet)
    {
        try
        {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG

            if (changeSet is ChangeSet<T> changes)
            {
                var idx = 0;
                foreach (var change in changes)
                {
                    WriteLog($"Adapting the change [{idx}/{changes.Count}] : {new { change.Reason, change.Type, change.Range }}");
                }
            }
            else
            {
                WriteLog($"Adapting changeset: {new { changeSet.TotalChanges, changeSet.Count, changeSet.Replaced, changeSet.Adds, changeSet.Refreshes, changeSet.Removes }}");
            }
#endif

            observableCollectionAdaptor.Adapt(changeSet);
        }
        catch (Exception e)
        {
            Log.Error($"Unhandled exception in adaptor, change: {changeSet} created at: {parentStackTraceInfo}", e);
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
            Log.Warn($"Full collection log:\n{log.DumpToTable("\n")}", e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
        }
    }
    
    
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> log = new();
    private readonly int maxLogLength = 30;
    private readonly Stopwatch sw = Stopwatch.StartNew();
    private string FormatPrefix()
    {
        return $"[{Thread.CurrentThread.ManagedThreadId,2}]";
    }
    
    private string FormatSuffix()
    {
        return $"[+{sw.ElapsedMilliseconds}ms] ";
    }

    private void WriteLog(string message)
    {
        while (log.Count > maxLogLength && log.TryDequeue(out var _))
        {
        }

        log.Enqueue($"{FormatPrefix()} {message} {FormatSuffix()}, stack: {(new StackTrace(1))}");
    }
#endif
}