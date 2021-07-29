using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.UI
{
    internal sealed class HotkeyListener : SharedResourceBase, IHotkeyListener
    {
        private readonly bool initialIsEnabled;
        private bool activated;
    
        public HotkeyListener(IHotkeyTracker hotkeyTracker)
        {
            Log = typeof(HotkeyListener).PrepareLogger().WithSuffix(hotkeyTracker);
            initialIsEnabled = hotkeyTracker.IsEnabled;
            hotkeyTracker.Reset();
            Log.Debug($"Initializing listener, tracker IsEnabled: {initialIsEnabled}");
                    
            hotkeyTracker.WhenAnyValue(x => x.IsActive)
                .Where(x => x)
                .Take(1)
                .SubscribeSafe(_ =>
                {
                    Log.Debug("Hotkey was pressed");
                    Activated = true;
                }, Log.HandleException)
                .AddTo(Anchors);
    
            Disposable.Create(() =>
            {
                Log.Debug("Disposing listener");
                if (hotkeyTracker.IsEnabled != initialIsEnabled)
                {
                    Log.Debug($"Restoring tracker IsEnabled to {initialIsEnabled}");
                    hotkeyTracker.IsEnabled = initialIsEnabled;
                }
                else
                {
                    Log.Debug($"Tracker IsEnabled is already in required state {initialIsEnabled}");
                }
    
                Log.Debug("Disposing listener");
            }).AddTo(Anchors);
                    
            if (!hotkeyTracker.IsEnabled)
            {
                Log.Debug("Enabling Tracker");
                hotkeyTracker.IsEnabled = true;
            }
        }
    
        private IFluentLog Log { get; }
    
        public bool Activated
        {
            get => activated;
            private set => RaiseAndSetIfChanged(ref activated, value);
        }
    }
}