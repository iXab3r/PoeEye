using System;
using System.Reactive.Disposables;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public class WorkerThread : DisposableReactiveObject
{
    private readonly Action<CancellationTokenSource> action;
    private readonly CancellationTokenSource consumerTokenSource;
    private readonly Thread consumerThread;
    
    public WorkerThread(string threadName, Action<CancellationTokenSource> action)
    {
        this.action = action;
        Log = GetType().PrepareLogger().WithSuffix($"WT {threadName}");
        Log.Info(() => $"Initializing buffered event log source");
        consumerTokenSource = new CancellationTokenSource();
        consumerThread = new Thread(() => DoWork(Log,consumerTokenSource, action))
        {
            IsBackground = true,
            Name = threadName
        };
        consumerThread.Start();

        Disposable.Create(() =>
        {
            Log.Debug(() => $"Sending signal to stop threads");
            try
            {
                consumerTokenSource.Cancel();
            }
            catch (Exception e)
            {
                Log.Warn("Failed to send signal gracefully", e);
            }

            Log.Debug(() => $"Disposed successfully");
        }).AddTo(Anchors);
        Log.Info(() => $"Initialization completed");
    }

    private static void DoWork(IFluentLog log, CancellationTokenSource cancellationTokenSource, Action<CancellationTokenSource> consumer)
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

    private IFluentLog Log { get; }
}