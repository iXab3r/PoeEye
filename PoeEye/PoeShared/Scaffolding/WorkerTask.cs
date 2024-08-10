using PropertyBinder;
using ReactiveUI;

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
        Log = GetType().PrepareLogger().WithSuffix(ToString);
        Log.Info($"Initializing new worker task");

        consumerTokenSource = new CancellationTokenSource();
        consumerTask = new Task(() => DoWork(Log, actionSupplier, consumerTokenSource.Token));

        Log.Info($"Worker task: {(consumerTask.Id)}");

        this.WhenAnyValue(x => x.Name)
            .WithPrevious()
            .Subscribe(x =>
            {
                if (string.IsNullOrEmpty(x.Current))
                {
                    if (string.IsNullOrEmpty(x.Previous))
                    {
                    }
                    else
                    {
                        Log.Info($"Worker task name is reset");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(x.Previous))
                    {
                        Log.Info($"Worker task name is set to {x.Current}");
                    }
                    else
                    {
                        Log.Info($"Worker task name changed to {x.Current}");
                    }
                }
            })
            .AddTo(Anchors);

        if (autoStart)
        {
            Log.Info($"Auto-starting the task");
            Start();
        }
    }

    public WorkerTask(
        Action<CancellationToken> action,
        bool autoStart = true) : this(actionSupplier: token =>
    {
        action(token);
        return Task.CompletedTask;
    }, autoStart)
    {
    }

    public string Name { get; set; }

    public TimeSpan TerminationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    private IFluentLog Log { get; }

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
                throw new InvalidStateException($"Failed to terminate the task in {TerminationTimeout}: {this}");
            }

            Log.Debug($"Disposed and started processing successfully");
        }).AddTo(Anchors);
    }

    private static void DoWork(IFluentLog log, Func<CancellationToken, Task> consumerSupplier, CancellationToken cancellationToken)
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

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.AppendParameterIfNotDefault(nameof(Name), Name);
        builder.AppendParameterIfNotDefault("TaskId", consumerTask != null ? consumerTask.Id.ToString() : "?");
    }
}