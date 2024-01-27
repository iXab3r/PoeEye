namespace PoeShared.Scaffolding;

public class WorkerThread : DisposableReactiveObject
{
    private readonly CancellationTokenSource consumerTokenSource;
    private readonly Thread consumerThread;
    private bool isRunning;

    public WorkerThread(
        string threadName,
        Func<CancellationToken, Task> actionSupplier,
        bool autoStart = true)
    {
        Log = GetType().PrepareLogger().WithSuffix($"WT {threadName}");
        Log.Info($"Initializing new worker thread");
        consumerTokenSource = new CancellationTokenSource();
        consumerThread = new Thread(() => DoWork(Log, consumerTokenSource.Token, actionSupplier))
        {
            IsBackground = true,
            Name = threadName
        };
        if (autoStart)
        {
            Log.Info($"Auto-starting the thread");
            Start();
        }
    }

    public WorkerThread(
        string threadName,
        Action<CancellationToken> action,
        bool autoStart = true) : this(threadName, actionSupplier: token =>
    {
        action(token);
        return Task.CompletedTask;
    }, autoStart)
    {
    }

    public TimeSpan TerminationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    private IFluentLog Log { get; }

    public void Start()
    {
        if (isRunning)
        {
            throw new InvalidOperationException("Thread is already running");
        }

        Log.Info($"Starting the thread");
        consumerThread.Start();
        isRunning = true;

        Disposable.Create(() =>
        {
            Log.Debug($"Sending signal to stop threads");
            try
            {
                consumerTokenSource.Cancel();
            }
            catch (Exception e)
            {
                Log.Warn("Failed to send signal gracefully", e);
            }

            if (TerminationTimeout > TimeSpan.Zero && !consumerThread.Join(TerminationTimeout))
            {
                throw new InvalidStateException($"Failed to terminated capture thread in {TerminationTimeout}");
            }

            Log.Debug($"Disposed and started processing successfully");
        }).AddTo(Anchors);
    }

    private static void DoWork(IFluentLog log, CancellationToken cancellationToken, Func<CancellationToken, Task> consumerSupplier)
    {
        try
        {
            log.Info("Thread has started");
            var consumer = consumerSupplier(cancellationToken);
            log.Info($"Thread consumer has been resolved, awaiting for completion");
            consumer.Wait(cancellationToken);
            log.Info(cancellationToken.IsCancellationRequested ? "Thread cancellation was requested" : "Thread consumer has completed its work without errors");
        }
        catch (OperationCanceledException)
        {
            log.Info("Thread was cancelled");
        }
        catch (Exception e)
        {
            log.Error("Thread encountered an exception", e);
            throw;
        }
        finally
        {
            log.Info("Thread is terminating");
        }
    }
}