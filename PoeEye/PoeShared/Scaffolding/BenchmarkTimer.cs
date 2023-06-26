using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using log4net;

namespace PoeShared.Scaffolding;

public sealed class BenchmarkTimer : DisposableReactiveObject
{
    private static readonly IFluentLog DefaultLogger = LogManager.GetLogger(typeof(BenchmarkTimer)).ToFluent();

    private readonly IFluentLog logger;
    private readonly string propertyName;
    private readonly ConcurrentQueue<string> operations = new ConcurrentQueue<string>();
    private TimeSpan previousOperationTimestamp;
    private readonly Stopwatch sw;
    private TimeSpan loggingElapsedThreshold = TimeSpan.Zero;
    private Func<bool> predicate = () => true;
    private bool logEachStep = true;
    private bool logOnDisposal = false;
    private FluentLogLevel logLevel = FluentLogLevel.Debug;

    public BenchmarkTimer(string benchmarkName, ILog logger, [CallerMemberName] string propertyName = null) : this(benchmarkName, logger?.ToFluent(), propertyName)
    {
    }
    
    public BenchmarkTimer(string benchmarkName, IFluentLog logger = null, [CallerMemberName] string propertyName = null) : this(logger, propertyName)
    {
        this.logger = logger ?? DefaultLogger;
        this.propertyName = propertyName ?? "unknown";
        sw = Stopwatch.StartNew();
        AddStep(() => $" => {benchmarkName}");
        Anchors.Add(() =>
        {
            if (logOnDisposal && sw.Elapsed > loggingElapsedThreshold)
            {
                LogMessage(() => $"[{propertyName}] [{sw.Elapsed.TotalMilliseconds:F1}ms] <= {benchmarkName}{(operations.Count <= 0 ? string.Empty : $"\n\t{string.Join("\n\t", operations)}")}");
            }
        });
    }
    
    public BenchmarkTimer(IFluentLog logger = null, [CallerMemberName] string propertyName = null)
    {
        this.logger = logger ?? DefaultLogger;
        this.propertyName = propertyName ?? "unknown";
        sw = Stopwatch.StartNew();
        Anchors.Add(() => sw.Stop());
    }
    
    public TimeSpan Elapsed => sw.Elapsed;

    public BenchmarkTimer ResetStep()
    {
        previousOperationTimestamp = sw.Elapsed;
        return this;
    }

    public BenchmarkTimer WithoutLoggingEachStep()
    {
        logEachStep = false;
        return this;
    }

    public BenchmarkTimer WithLoggingEachStep()
    {
        logEachStep = true;
        return this;
    }
    
    public BenchmarkTimer WithLogLevel(FluentLogLevel newLogLevel)
    {
        this.logLevel = newLogLevel;
        return this;
    }
        
    public BenchmarkTimer WithoutLoggingOnDisposal()
    {
        logOnDisposal = false;
        return this;
    }

    public BenchmarkTimer WithLoggingOnDisposal()
    {
        logOnDisposal = true;
        return this;
    }
        
    public BenchmarkTimer WithCondition([NotNull] Func<bool> predicate)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    public BenchmarkTimer WithMinElapsedThreshold(TimeSpan minElapsedThreshold)
    {
        loggingElapsedThreshold = minElapsedThreshold;
        return this;
    }

    public BenchmarkTimer Step(Func<string> messageFactory)
    {
        AddStep(messageFactory);
        return this;
    }
        
    public BenchmarkTimer Step(string message)
    {
        return Step(() => message);
    }

    private void AddStep(Func<string> messageFactory)
    {
        AddStep(messageFactory, sw.Elapsed);
    }
    
    private void AddStep(Func<string> messageFactory, TimeSpan elapsed)
    {
        if (predicate != null && !predicate())
        {
            return;
        }

        var logMessage = $"[{propertyName}] [{(sw.Elapsed - previousOperationTimestamp).TotalMilliseconds:F1}ms] {messageFactory()}";
        operations.Enqueue(logMessage);
        if (logEachStep && logger.IsDebugEnabled)
        {
            LogMessage(() => logMessage);
        }
        previousOperationTimestamp = elapsed;
    }

    private void LogMessage(Func<string> messageSupplier)
    {
        switch (logLevel)
        {
            case FluentLogLevel.Trace:
            case FluentLogLevel.Debug:
                logger.Debug(messageSupplier);
                break;
            case FluentLogLevel.Info:
                logger.Info(messageSupplier);
                break;
            case FluentLogLevel.Warn:
                logger.Warn(messageSupplier);
                break;
            case FluentLogLevel.Error:
                logger.Error(messageSupplier);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}