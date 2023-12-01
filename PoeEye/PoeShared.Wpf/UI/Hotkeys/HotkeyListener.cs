using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.UI;

internal sealed class HotkeyListener : SharedResourceBase<HotkeyListener>, IHotkeyListener
{
    private readonly bool initialIsEnabled;
    
    public HotkeyListener(IHotkeyTracker hotkeyTracker)
    {
        Log = base.Log.WithSuffix(hotkeyTracker);
        initialIsEnabled = hotkeyTracker.IsEnabled;
        hotkeyTracker.Reset();
        Log.Debug(() => $"Initializing listener, tracker IsEnabled: {initialIsEnabled}");

        WhenActivated = hotkeyTracker.WhenAnyValue(x => x.IsActive)
            .Where(x => x)
            .Take(1)
            .Do(_ => Log.Debug("Hotkey was pressed"))
            .ToUnit()
            .Publish()
            .RefCount();
    
        Disposable.Create(() =>
        {
            Log.Debug("Disposing listener");
            if (hotkeyTracker.IsEnabled != initialIsEnabled)
            {
                Log.Debug(() => $"Restoring tracker IsEnabled to {initialIsEnabled}");
                hotkeyTracker.IsEnabled = initialIsEnabled;
            }
            else
            {
                Log.Debug(() => $"Tracker IsEnabled is already in required state {initialIsEnabled}");
            }
    
            Log.Debug("Disposed listener");
        }).AddTo(Anchors);
                    
        if (!hotkeyTracker.IsEnabled)
        {
            Log.Debug("Enabling Tracker");
            hotkeyTracker.IsEnabled = true;
        }
    }
    
    private new IFluentLog Log { get; }
    
    public IObservable<Unit> WhenActivated { get; }
}