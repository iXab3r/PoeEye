using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Windows.Threading;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Scaffolding.WPF;

internal sealed class UiSharedResourceLatch : DisposableReactiveObject, IUiSharedResourceLatch
{
    private static readonly Binder<UiSharedResourceLatch> Binder = new();
    private readonly Dispatcher dispatcher;
    private readonly SerialDisposable isBusyAnchor;

    private readonly ISharedResourceLatch isBusyLatch;
    private readonly ISharedResourceLatch pauseLatch;
    private readonly IScheduler uiIdleScheduler;

    static UiSharedResourceLatch()
    {
        Binder.Bind(x => x.pauseLatch.IsBusy).To(x => x.IsPaused);
    }

    public UiSharedResourceLatch(
        [Dependency(WellKnownDispatchers.UI)] Dispatcher dispatcher,
        [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiIdleScheduler)
    {
        Log = typeof(UiSharedResourceLatch).PrepareLogger("UiLatch");
        this.dispatcher = dispatcher;
        this.uiIdleScheduler = uiIdleScheduler;
        this.isBusyAnchor = new SerialDisposable().AddTo(Anchors);
        this.pauseLatch = new SharedResourceLatch($"UI-Pause").AddTo(Anchors);
        this.isBusyLatch = new SharedResourceLatch($"UI-IsBusy").AddTo(Anchors);
        Log.Info($"UI latch is initialized");

        isBusyLatch.WhenAnyValue(x => x.IsBusy)
            .SubscribeSafe(HandleIsBusyChange, Log.HandleUiException)
            .AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    private IFluentLog Log { get; }

    public bool IsBusy { get; private set; }

    public bool IsPaused { get; [UsedImplicitly] private set; }

    public IDisposable Busy()
    {
        return isBusyLatch.Rent();
    }

    public IDisposable Pause()
    {
        return pauseLatch.Rent();
    }

    private void HandleIsBusyChange()
    {
        // FIXME Rewrite me PLEASE ! Idea behind this pile of shit is the following: 
        // 0) IsBusy controls visibility of BusyDecorator that is shown on a separate UI thread to allow showing animated loading indicator when main UI thread is processing something
        // 1) If IsBusy is set to TRUE is must be propagated to UI asap => so either Invoke or simply assign
        // 2) If IsBusy is set to FALSE it must be sent to UI thread, but only as a low-priority task, this will make UI thread process all other messages before
        // 3) If there are multiple assignments next one should cancel previous, otherwise it will make state unreliable (e.g. IsBusy = false (sends to queue with low-priority), then IsBusy = true(executed on the same thread) will not work correctly
        var isOnDispatcher = dispatcher.CheckAccess();
        Log.Debug(() => $"Processing IsBusy: {isBusyLatch.IsBusy} {isBusyLatch}, isOnDispatcher: {isOnDispatcher}");
        if (isBusyAnchor.Disposable != null)
        {
            Log.Debug(() => $"Clearing previous anchors");
            isBusyAnchor.Disposable = null;
            Log.Debug(() => $"Cleared previous anchors");
        }
            
        switch (isBusyLatch.IsBusy)
        {
            case true when isOnDispatcher:
                Log.Debug(() => $"Changing IsBusy(on dispatcher): {IsBusy} => true");
                IsBusy = true;
                Log.Debug(() => $"Changed IsBusy(on dispatcher) to {IsBusy}");
                break;
            case true:
                Log.Debug(() => $"Invoking operation to change IsBusy to true");
                dispatcher.Invoke(HandleIsBusyChange);
                Log.Debug(() => $"Invocation completed for operation to change IsBusy to true");
                break;
            case false:
                Log.Debug("Scheduling IsBusy reset");
                isBusyAnchor.Disposable = uiIdleScheduler.Schedule(() =>
                {
                    Log.Info($"Resetting IsBusy: {IsBusy} => false");
                    IsBusy = false;
                    Log.Info($"Reset IsBusy to {IsBusy}");
                });
                Log.Debug("Scheduled IsBusy reset");
                break;
        }
    }
}