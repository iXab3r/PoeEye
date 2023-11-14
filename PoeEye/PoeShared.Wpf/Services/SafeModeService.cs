using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace PoeShared.Services;

internal sealed class SafeModeService : DisposableReactiveObjectWithLogger, ISafeModeService
{
    private readonly IUiSharedResourceLatch resourceLatch;

    public SafeModeService(
        IUiSharedResourceLatch resourceLatch)
    {
        this.resourceLatch = resourceLatch;
        Log.Info($"Safe-Mode service is initialized");

        this.WhenAnyValue(x => x.IsInSafeMode)
            .Select(x => x ? Observable.Using(() =>
            {
                Log.Warn("Entering Pause due to Safe-Mode");
                var pauseAnchor = resourceLatch.Pause();
                return new CompositeDisposable() { pauseAnchor, Disposable.Create(() => Log.Warn("Exiting Pause due to Safe-Mode")) };
            }, disposable => Observable.Never<Unit>()) : Observable.Return(Unit.Default))
            .Switch()
            .Subscribe()
            .AddTo(Anchors);
    }

    public bool IsInSafeMode { get; private set; }
    
    public void EnterSafeMode()
    {
        if (IsInSafeMode)
        {
            return;
        }
        Log.Info("Entering safe mode");
        IsInSafeMode = true;
        Log.Info("Entering safe mode");
    }
    
    public void ExitSafeMode()
    {
        if (!IsInSafeMode)
        {
            return;
        }
        Log.Info("Exiting safe mode");
        IsInSafeMode = false;
        Log.Info("Exited safe mode");
    }
}