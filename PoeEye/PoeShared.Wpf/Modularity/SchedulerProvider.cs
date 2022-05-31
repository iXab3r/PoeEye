using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using Unity;

namespace PoeShared.Modularity;

public sealed class SchedulerProvider : DisposableReactiveObject, ISchedulerProvider
{
    private static readonly IFluentLog Log = typeof(SchedulerProvider).PrepareLogger();

    private static readonly Lazy<SchedulerProvider> InstanceSupplier = new();

    private readonly ConcurrentDictionary<string, IScheduler> schedulers = new();

    public static ISchedulerProvider Instance => InstanceSupplier.Value;

    public static IScheduler RedirectToUiScheduler => Instance.GetOrAdd(WellKnownSchedulers.RedirectToUI);

    public void Initialize(IUnityContainer container)
    {
        schedulers[WellKnownSchedulers.Background] = container.Resolve<IScheduler>(WellKnownSchedulers.Background);
        schedulers[WellKnownSchedulers.UI] = container.Resolve<IScheduler>(WellKnownSchedulers.UI);
        schedulers[WellKnownSchedulers.RedirectToUI] = container.Resolve<IScheduler>(WellKnownSchedulers.RedirectToUI);
        schedulers[WellKnownSchedulers.UIIdle] = container.Resolve<IScheduler>(WellKnownSchedulers.UIIdle);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public IScheduler GetOrAdd(string name)
    {
        Log.Debug(() => $"Retrieving scheduler...");
        return schedulers.GetOrAdd(name, x => CreateEnforcedThreadScheduler(name, ThreadPriority.Normal));
    }
    
    public IScheduler Add(string name, ThreadPriority threadPriority)
    {
        if (schedulers.TryGetValue(name, out var existing))
        {
            throw new InvalidOperationException($"Scheduler with the same name {name} is already created: {existing}");
        }

        var newScheduler = CreateEnforcedThreadScheduler(name, priority: threadPriority);
        if (!schedulers.TryAdd(name, newScheduler))
        {
            throw new InvalidOperationException($"Failed to add scheduler {name} to collection: {schedulers.DumpToString()}");
        }
        return newScheduler;
    }

    private IScheduler CreateEnforcedThreadScheduler(string name, ThreadPriority priority)
    {
        Guard.ArgumentNotNull(name, nameof(name));

        Log.WithSuffix(name).Debug(() => $"Creating new enforced thread scheduler");
        return new EnforcedThreadScheduler(name, priority);
    }

    private IScheduler CreateDispatcherScheduler(string name)
    {
        Guard.ArgumentNotNull(name, nameof(name));

        Log.WithSuffix(name).Debug(() => $"Creating new dispatcher");
        var consumer = new TaskCompletionSource<IScheduler>();
        var dispatcherThread = new Thread(InitializeDispatcherThread)
        {
            Name = $"SC-{name}",
            IsBackground = true
        };
        dispatcherThread.SetApartmentState(ApartmentState.STA);
        Log.WithSuffix(name).Debug(() => $"Starting dispatcher thread");
        dispatcherThread.Start(consumer);
        Log.WithSuffix(name).Debug(() => $"Dispatcher thread started");
        return consumer.Task.Result;
    }

    private void InitializeDispatcherThread(object arg)
    {
        if (arg is not TaskCompletionSource<IScheduler> consumer)
        {
            throw new InvalidOperationException($"Wrong args: {arg}");
        }

        InitializeDispatcherThread(consumer);
    }

    private void InitializeDispatcherThread(TaskCompletionSource<IScheduler> consumer)
    {
        try
        {
            Log.Debug("Dispatcher thread started");
            var dispatcher = Dispatcher.CurrentDispatcher;
            Log.Debug(() => $"Dispatcher: {dispatcher}");
            var scheduler = new DispatcherScheduler(dispatcher);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationStarted += h,
                    h => scheduler.Dispatcher.Hooks.OperationStarted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationStarted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(Anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(Anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationAborted += h,
                    h => scheduler.Dispatcher.Hooks.OperationAborted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationAborted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(Anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(Anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPosted += h,
                    h => scheduler.Dispatcher.Hooks.OperationPosted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPosted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(Anchors);
            Log.Debug(() => $"Scheduler: {dispatcher}");
            consumer.TrySetResult(scheduler);

            Log.Debug("Starting dispatcher...");
            Dispatcher.Run();
        }
        catch (Exception e)
        {
            Log.HandleException(e);
            consumer.TrySetException(e);
        }
        finally
        {
            Log.Debug("Dispatcher thread completed");
        }
    }

    private static void LogEvent(string eventName, DispatcherHookEventArgs eventArgs)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug(
                $"[{eventName}] Priority: {eventArgs.Operation.Priority} Status: {eventArgs.Operation.Status}, Operation: {eventArgs.Operation.Task}");
        }
    }
}