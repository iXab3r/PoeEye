using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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

    public static IScheduler RedirectToUiScheduler
    {
        get
        {
            if (Instance.TryGet(WellKnownSchedulers.RedirectToUI, out var result))
            {
                return result;
            }

            throw new InvalidOperationException("Redirect to UI scheduler is not initialized yet");
        }
    }

    public void Initialize(IUnityContainer container)
    {
        Add(WellKnownSchedulers.Background, container.Resolve<IScheduler>(WellKnownSchedulers.Background));
        Add(WellKnownSchedulers.UI, container.Resolve<IScheduler>(WellKnownSchedulers.UI));
        Add(WellKnownSchedulers.RedirectToUI, container.Resolve<IScheduler>(WellKnownSchedulers.RedirectToUI));
        Add(WellKnownSchedulers.UIIdle, container.Resolve<IScheduler>(WellKnownSchedulers.UIIdle));
    }

    public IScheduler GetOrAdd(string name)
    {
        Log.Debug($"Retrieving scheduler {name}");
        return schedulers.GetOrAdd(name, x => CreateDispatcherScheduler(name, ThreadPriority.Normal));
    }

    public DispatcherScheduler GetOrAddDispatcherScheduler(string name)
    {
        var scheduler = GetOrAdd(name);
        if (scheduler is not DispatcherScheduler dispatcherScheduler)
        {
            throw new InvalidOperationException($"Scheduler {name} is not a dispatcher scheduler: {scheduler}");
        }

        return dispatcherScheduler;
    }

    public bool TryGet(string name, out IScheduler scheduler)
    {
        Log.Debug($"Trying to retrieve scheduler {name}");
        var result = schedulers.TryGetValue(name, out scheduler);
        if (result)
        {
            Log.Debug($"Retrieved scheduler {name}: {scheduler}");
        }
        else
        {
            Log.Warn($"Failed to retrieve scheduler {name}");
        }
        return result;
    }

    public IScheduler Add(string name, ThreadPriority threadPriority)
    {
        if (schedulers.TryGetValue(name, out var existing))
        {
            throw new InvalidOperationException($"Scheduler with the same name {name} is already created: {existing}");
        }

        var newScheduler = CreateDispatcherScheduler(name, priority: threadPriority);
        return Add(name, newScheduler);
    }

    public Dispatcher AddDispatcher(string name, ThreadPriority threadPriority)
    {
        if (schedulers.TryGetValue(name, out var existing))
        {
            throw new InvalidOperationException($"Scheduler with the same name {name} is already created: {existing}");
        }

        var newScheduler = CreateDispatcherScheduler(name, threadPriority);
        Add(name, newScheduler);
        return newScheduler.Dispatcher;
    }

    public IScheduler Add(string name, IScheduler scheduler)
    {
        if (!schedulers.TryAdd(name, scheduler))
        {
            Log.Warn($"Failed to add scheduler {name} to collection: {schedulers.DumpToString()}");
        }
        
        schedulers[name] = scheduler;
        return scheduler;
    }

    public DispatcherScheduler CreateDispatcherScheduler(string name, ThreadPriority priority)
    {
        return DispatcherScheduler.CreateDispatcherScheduler(name, priority);
    }

    private static IScheduler CreateEnforcedThreadScheduler(string name, ThreadPriority priority)
    {
        Guard.ArgumentNotNull(name, nameof(name));

        Log.WithSuffix(name).Debug($"Creating new enforced thread scheduler");
        return new EnforcedThreadScheduler(name, priority);
    }

   

    private static IDisposable Listen(DispatcherScheduler scheduler)
    {
          using var anchors = new CompositeDisposable();
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationStarted += h,
                    h => scheduler.Dispatcher.Hooks.OperationStarted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationStarted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationAborted += h,
                    h => scheduler.Dispatcher.Hooks.OperationAborted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationAborted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                    h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(anchors);
            Observable
                .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                    h => scheduler.Dispatcher.Hooks.OperationPosted += h,
                    h => scheduler.Dispatcher.Hooks.OperationPosted -= h)
                .SubscribeSafe(eventArgs => LogEvent("OperationPosted", eventArgs.EventArgs), Log.HandleUiException)
                .AddTo(anchors);
            return anchors;
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