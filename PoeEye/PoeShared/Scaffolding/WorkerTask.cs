namespace PoeShared.Scaffolding;

public class WorkerTask : DisposableReactiveObject
{
    private readonly CancellationTokenSource consumerTokenSource;
    private readonly Task consumerTask;
    private bool isRunning;

    public WorkerTask(
        Func<CancellationToken, Task> actionSupplier,
        bool autoStart = true)
    {
        Log = GetType().PrepareLogger();
        Log.Info($"Initializing new worker task");
        consumerTokenSource = new CancellationTokenSource();
        consumerTask = new Task(() => DoWork(Log, consumerTokenSource.Token, actionSupplier));
        if (autoStart)
        {
            Log.Info($"Auto-starting the task");
            Start();
        }
    }
    
    private IFluentLog Log { get; }

    public WorkerTask(
        Action<CancellationToken> action,
        bool autoStart = true) : this(actionSupplier: token =>
    {
        action(token);
        return Task.CompletedTask;
    }, autoStart)
    {
    }
    
    public TimeSpan TerminationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public void Start()
    {
        if (isRunning)
        {
            throw new InvalidOperationException("Task is already running");
        }

        Log.Info($"Starting the task");
        consumerTask.Start();
        isRunning = true;

        Disposable.Create(() =>
        {
            Log.Debug($"Sending signal to stop task");
            try
            {
                consumerTokenSource.Cancel();
            }
            catch (Exception e)
            {
                Log.Warn("Failed to send signal gracefully", e);
            }

            if (TerminationTimeout > TimeSpan.Zero && !consumerTask.Wait(TerminationTimeout))
            {
                throw new InvalidStateException($"Failed to terminate task in {TerminationTimeout}");
            }
            Log.Debug($"Disposed and started processing successfully");
        }).AddTo(Anchors);
    }
    
    
    private static void DoWork(IFluentLog log, CancellationToken cancellationToken, Func<CancellationToken, Task> consumerSupplier)
    {
        try
        {
            log.Debug("Task has started");
            var consumer = consumerSupplier(cancellationToken);
            log.Debug($"Task consumer has been resolved, awaiting for completion");
            consumer.Wait(cancellationToken);
            log.Debug(cancellationToken.IsCancellationRequested ? "Thread cancellation was requested" : "Thread consumer has completed its work without errors");
        }
        catch (OperationCanceledException)
        {
            log.Debug("Task was cancelled");
        }
        catch (Exception e)
        {
            log.Error("Task encountered an exception", e);
            throw;
        }
        finally
        {
            log.Debug("Task is terminating");
        }
    }
}