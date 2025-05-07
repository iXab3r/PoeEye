#if DEBUG && false
#define OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
#endif

using System.Collections.Immutable;
using System.Collections.Specialized;
using DynamicData.Binding;
using JetBrains.Annotations;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Scaffolding;

public sealed class ObservableCollectionEx<T> : DisposableReactiveObjectWithLogger, IObservableCollection<T>, IReadOnlyObservableCollection<T>
{
    private readonly ObservableCollectionExtended<T> collection = new();

    private static readonly Binder<ObservableCollectionEx<T>> Binder = new();

    private readonly int parentThread = Environment.CurrentManagedThreadId;
    private readonly string parentStackTraceInfo = new StackTrace().ToString();

    static ObservableCollectionEx()
    {
        Binder.Bind(x => x.collection.Count).To(x => x.Count);
    }

    public ObservableCollectionEx(IEnumerable<T> enumerable) : this()
    {
        collection.AddRange(enumerable);
    }

    public ObservableCollectionEx()
    {
        Log.AddSuffix($"<{typeof(T).Name}>");
        Log.AddSuffix($"TID {parentThread}");
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Collection of type {typeof(T)} is created @ {parentStackTraceInfo}");
#endif
        collection.CollectionChanged += OnCollectionChanged;
        Binder.Attach(this).AddTo(Anchors);
    }

    public int Count { get; [UsedImplicitly] private set; }

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        var clone = collection.ToImmutableList();
        return clone.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var clone = collection.ToImmutableList();
        return ((IEnumerable) clone).GetEnumerator();
    }

    public void Add(T item)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Add: {new {item}}");
#endif

        try
        {
            collection.Add(item);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void Clear()
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Clear");
#endif

        try
        {
            collection.Clear();
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public bool Contains(T item)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Contains: {new {item}}");
#endif

        try
        {
            return collection.Contains(item);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"CopyTo: {new {arrayIndex, array}}");
#endif

        try
        {
            collection.CopyTo(array, arrayIndex);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public bool Remove(T item)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Remove: {new {item}}");
#endif

        try
        {
            return collection.Remove(item);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public int IndexOf(T item)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"IndexOf: {new {item}}");
#endif

        try
        {
            return collection.IndexOf(item);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void Insert(int index, T item)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Insert: {new {index, item}}");
#endif

        try
        {
            collection.Insert(index, item);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void RemoveAt(int index)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"RemoveAt: {new {index}}");
#endif

        try
        {
            collection.RemoveAt(index);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public T this[int index]
    {
        get => collection[index];
        set => collection[index] = value;
    }

    public IDisposable SuspendCount()
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"SuspendCount");
#endif

        try
        {
            var result = collection.SuspendCount();
            return new CompositeDisposable()
            {
                result,
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
                Disposable.Create(() => WriteLog($"ResumeCount released"))
#endif
            };
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public IDisposable SuspendNotifications()
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"SuspendNotifications");
#endif

        try
        {
            var result = collection.SuspendNotifications();
            return new CompositeDisposable()
            {
                result,
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
                Disposable.Create(() => WriteLog($"ResumeNotifications"))
#endif
            };
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void Load(IEnumerable<T> items)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Load items");
#endif

        try
        {
            collection.Load(items);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public void Move(int oldIndex, int newIndex)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"Move: {new {oldIndex, newIndex}}");
#endif

        try
        {
            collection.Move(oldIndex, newIndex);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        WriteLog($"CollectionChanged: {new {args.Action, args.OldStartingIndex, args.NewStartingIndex, OldItemsCount = args.OldItems?.Count, NewItemsCount = args.NewItems?.Count}}");
#endif
        try
        {
            CollectionChanged?.Invoke(this, args);
        }
        catch (Exception e)
        {
            HandleException(e);
            throw;
        }
    }

    private void HandleException(Exception e)
    {
        Log.Error($"Unhandled exception in collection, collection created at: {parentStackTraceInfo}", e);
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        Log.Warn($"Full collection log:\n{log.DumpToTable("\n")}", e);
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
#endif
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
        return $"[{Count} items] [+{sw.ElapsedMilliseconds}ms] ";
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