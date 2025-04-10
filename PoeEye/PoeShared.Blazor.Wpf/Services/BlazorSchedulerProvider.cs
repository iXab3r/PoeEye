using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class BlazorSchedulerProvider : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(BlazorSchedulerProvider).PrepareLogger();

    private static readonly Lazy<BlazorSchedulerProvider> InstanceSupplier = new();

    private readonly ConcurrentDictionary<string, IScheduler> schedulers = new();

    public static BlazorSchedulerProvider Instance => InstanceSupplier.Value;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public IScheduler GetOrAdd(string name)
    {
        Log.Debug($"Retrieving scheduler {name}");
        return schedulers.GetOrAdd(name, x => CreateDispatcherScheduler(name, ThreadPriority.Normal));
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
            throw new InvalidOperationException($"Failed to add scheduler {name} to collection: {schedulers.DumpToString()}");
        }

        return scheduler;
    }

    public DispatcherScheduler CreateDispatcherScheduler(string name, ThreadPriority priority)
    {
        return CreateDispatcherSchedulerInternal(name, priority);
    }

    private static DispatcherScheduler CreateDispatcherSchedulerInternal(string name, ThreadPriority priority)
    {
        Guard.ArgumentNotNull(name, nameof(name));

        Log.WithSuffix(name).Debug($"Creating new dispatcher");
        var consumer = new TaskCompletionSource<DispatcherScheduler>();
        var dispatcherThread = new Thread(InitializeDispatcherThread)
        {
            Name = $"S#{name}",
            Priority = priority,
            IsBackground = true
        };
        dispatcherThread.SetApartmentState(ApartmentState.STA);
        Log.WithSuffix(name).Debug($"Starting dispatcher thread");
        dispatcherThread.Start(consumer);
        Log.WithSuffix(name).Debug($"Dispatcher thread started");
        return consumer.Task.Result;
    }

    private static void InitializeDispatcherThread(object arg)
    {
        if (arg is not TaskCompletionSource<DispatcherScheduler> consumer)
        {
            throw new InvalidOperationException($"Wrong args: {arg}");
        }

        RunDispatcherThread(consumer);
    }

    private static void RunDispatcherThread(TaskCompletionSource<DispatcherScheduler> consumer)
    {
        try
        {
            Log.Debug("Dispatcher thread started");
            var dispatcher = Dispatcher.CurrentDispatcher;
            Log.Debug($"Dispatcher: {dispatcher}");
            var scheduler = new DispatcherScheduler(dispatcher); 
          
            Log.Debug($"Scheduler: {dispatcher}");
            consumer.TrySetResult(scheduler);

            Log.Debug("Starting dispatcher...");
            Dispatcher.Run();
        }
        catch (Exception e)
        {
            Log.HandleException(e);
            consumer.TrySetException(e);
            throw; 
        }
        finally
        {
            Log.Debug("Dispatcher thread completed");
        }
    }
}