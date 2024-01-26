namespace PoeShared.Scaffolding;

public class WorkerThread : DisposableReactiveObject
{
    private readonly Action<CancellationToken> action;
    private readonly CancellationTokenSource consumerTokenSource;
    private readonly Thread consumerThread;
    private bool isRunning;

    public WorkerThread(
        string threadName,
        Action<CancellationToken> action,
        bool autoStart = true)
    {
        this.action = action;
        Log = GetType().PrepareLogger().WithSuffix($"WT {threadName}");
        Log.Info($"Initializing buffered event log source");
        consumerTokenSource = new CancellationTokenSource();
        consumerThread = new Thread(() => DoWork(Log, consumerTokenSource.Token, action))
        {
            IsBackground = true,
            Name = threadName
        };
        Log.Info($"Initialization completed");
        if (autoStart)
        {
            Log.Info($"Auto-starting the thread");
            Start();
        }
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

    private static void DoWork(IFluentLog log, CancellationToken cancellationTokenSource, Action<CancellationToken> consumer)
    {
        try
        {
            log.Info("Thread has started");
            consumer(cancellationTokenSource);
            if (cancellationTokenSource.IsCancellationRequested)
            {
                log.Info("Thread cancellation was requested");
            }
            else
            {
                log.Info("Thread consumer has completed its work");
            }
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
            log.Info("Thread has completed");
        }
    }
}