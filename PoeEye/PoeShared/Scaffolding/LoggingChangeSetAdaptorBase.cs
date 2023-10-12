namespace PoeShared.Scaffolding;

public abstract class LoggingChangeSetAdaptorBase : DisposableReactiveObject
{
    protected readonly int ParentThread = Environment.CurrentManagedThreadId;
    protected readonly string ParentStackTraceInfo = new StackTrace().ToString();
    protected readonly ConcurrentQueue<string> Messages = new();
    
    private readonly int maxLogLength = 30;
    private readonly Stopwatch sw = Stopwatch.StartNew();
    
    public IFluentLog Logger { get; set; }

    public FluentLogLevel LogLevel { get; set; } = FluentLogLevel.Debug;

    protected string FormatPrefix()
    {
        return $"[{Thread.CurrentThread.ManagedThreadId,2}]";
    }

    protected string FormatSuffix()
    {
        return $"[+{sw.ElapsedMilliseconds}ms] ";
    }

    protected void WriteLog(string message)
    {
        while (Messages.Count > maxLogLength && Messages.TryDequeue(out var _))
        {
        }

        Messages.Enqueue($"{FormatPrefix()} {message} {FormatSuffix()}, stack: {(new StackTrace(1))}");
        Logger?.Write(LogLevel, () => $"{message} {FormatSuffix()}");
    }
}