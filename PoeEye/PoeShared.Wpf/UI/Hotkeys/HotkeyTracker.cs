using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media.Animation;
using DynamicData;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using Unity;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class HotkeyTracker : DisposableReactiveObject, IHotkeyTracker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyTracker));
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private readonly SourceCache<HotkeyGesture, HotkeyGesture> hotkeysSource = new(x => x);
        private readonly IClock clock;

        private HotkeyGesture hotkey;
        private HotkeyMode hotkeyMode;
        private bool suppressKey;
        private bool isActive;

        public HotkeyTracker(
           IClock clock,
           IKeyboardEventsSource eventSource,
           [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker)
        {
            this.clock = clock;
            Disposable
                .Create(() => Log.Debug($"Disposing HotkeyTracker, gesture: {Hotkey} (mode: {HotkeyMode})"))
                .AddTo(Anchors);
            IsActive = true;

            hotkeysSource
                .Connect()
                .Bind(out var hotkeys)
                .Subscribe()
                .AddTo(Anchors);
            Hotkeys = hotkeys;

            this.WhenAnyValue(x => x.Hotkey)
                .WithPrevious((prev, curr) => new { prev, curr })
                .SubscribeSafe(
                    x =>
                    {
                        if (x.curr == null || x.curr.IsEmpty)
                        {
                            Log.Debug($"Hotkey tracking disabled (hotkey gesture {x.prev} => {x.curr})");
                            IsActive = false;
                        }
                        else
                        {
                            Log.Debug($"Tracking hotkey changed (hotkey gesture {x.prev} => {x.curr})");
                        }
                    }, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Hotkey)
                .Select(hotkey => hotkey == null ? Observable.Empty<HotkeyData>() : BuildHotkeySubscription(eventSource))
                .Switch()
                .DistinctUntilChanged(x => new { x.Hotkey, x.KeyDown, x.Timestamp }) // removed possible duplicates from multiple sources
                .Where(
                    hotkeyData =>
                    {
                        /*
                         * This method MUST be executed on the same thread which emitted Key/Mouse event
                         * otherwise .Handled value will be ignored due to obvious concurrency reasons
                         */
                        if (mainWindowTracker.ActiveProcessId != CurrentProcessId)
                        {
                            Log.Debug($"Application is NOT active, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey},  configuredKey: {Hotkey}, mode: {HotkeyMode})");
                            if (suppressKey)
                            {
                                hotkeyData.MarkAsHandled();
                            }
                            return true;
                        }

                        Log.Debug($"Application is active, skipping hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey},  configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        return false;
                    })
                .DistinctUntilChanged(x => new { x.Hotkey, x.KeyDown }) // disables "blinking" when hotkey is held
                .SubscribeSafe(
                    hotkeyData =>
                    {
                        Log.Debug($"Hotkey {hotkeyData.Hotkey} pressed, state: {(hotkeyData.KeyDown ? "down" : "up")}, suppressed: {suppressKey}");

                        if (HotkeyMode == HotkeyMode.Click)
                        {
                            if (hotkeyData.KeyDown)
                            {
                                IsActive = !IsActive;
                            }
                        }
                        else
                        {
                            IsActive = !IsActive;
                        }
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);
        }

        public bool IsActive
        {
            get => isActive;
            set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public HotkeyGesture Hotkey
        {
            get => hotkey;
            set => RaiseAndSetIfChanged(ref hotkey, value);
        }

        public ReadOnlyObservableCollection<HotkeyGesture> Hotkeys { get; }

        public HotkeyMode HotkeyMode
        {
            get => hotkeyMode;
            set => RaiseAndSetIfChanged(ref hotkeyMode, value);
        }

        public bool SuppressKey
        {
            get => suppressKey;
            set => RaiseAndSetIfChanged(ref suppressKey, value);
        }

        public void Add(HotkeyGesture hotkeyToAdd)
        {
            Log.Debug($"Registering hotkey {hotkeyToAdd}, current list: {Hotkeys.DumpToTextRaw()}");
            hotkeysSource.AddOrUpdate(hotkeyToAdd);
        }

        public void Remove(HotkeyGesture hotkeyToRemove)
        {
            Log.Debug($"Unregistering hotkey {hotkeyToRemove}, current list: {Hotkeys.DumpToTextRaw()}");
            hotkeysSource.RemoveKey(hotkeyToRemove);
        }

        public void Clear()
        {
            Log.Debug($"Unregistering all hotkeys, hotkey: {hotkey}, current list: {Hotkeys.DumpToTextRaw()}");
            Hotkey = HotkeyGesture.Empty;
            hotkeysSource.Clear();
        }

        private bool IsConfiguredHotkey(HotkeyData data)
        {
            if (data.Hotkey == null || data.Hotkey.IsEmpty)
            {
                return false;
            }
            
            Log.Debug($"Hotkey {(data.KeyDown ? "pressed" : "released")}: {data.Hotkey}");
            return data.Hotkey.Equals(hotkey) || hotkeysSource.Items.Any(x => data.Hotkey.Equals(x));
        }

        public override string ToString()
        {
            return $"HotkeyTracker for {hotkeysSource.Items.Concat(new []{ hotkey }).DumpToTextRaw()}";
        }

        private IObservable<HotkeyData> BuildHotkeySubscription(
            IKeyboardEventsSource eventSource)
        {
            var hotkeyDown =
                Observable.Merge(
                        eventSource.WhenMouseDown.Select(x => HotkeyData.FromEvent(x, clock.UtcNow)),
                        eventSource.WhenKeyDown.Select(x => HotkeyData.FromEvent(x, clock.UtcNow)))
                    .Select(x => x.SetKeyDown(true))
                    .Where(IsConfiguredHotkey);
            
            var hotkeyUp =
                Observable.Merge(
                        eventSource.WhenMouseUp.Select(x => HotkeyData.FromEvent(x, clock.UtcNow)),
                        eventSource.WhenKeyUp.Select(x => HotkeyData.FromEvent(x, clock.UtcNow)))
                    .Select(x => x.SetKeyDown(false))
                    .Where(IsConfiguredHotkey);

            return hotkeyDown
                .Merge(hotkeyUp);
        }

        private struct HotkeyData
        {
            public KeyEventArgs KeyEventArgs { get; set; }

            public MouseEventArgs MouseEventArgs { get; set; }

            public HotkeyGesture Hotkey { get; set; }

            public bool KeyDown { get; set; }
            
            public DateTime Timestamp { get; set; }

            public HotkeyData SetKeyDown(bool value)
            {
                KeyDown = value;
                return this;
            }

            public HotkeyData MarkAsHandled()
            {
                if (KeyEventArgs != null)
                {
                    KeyEventArgs.Handled = true;
                }

                if (MouseEventArgs is MouseEventExtArgs mouseEventExtArgs)
                {
                    mouseEventExtArgs.Handled = true;
                }

                return this;
            }

            public static HotkeyData FromEvent(MouseEventArgs args, DateTime timestamp)
            {
                return new HotkeyData
                {
                    Hotkey = new HotkeyGesture(args.Button),
                    MouseEventArgs = args,
                    Timestamp = timestamp
                };
            }

            public static HotkeyData FromEvent(KeyEventArgs args, DateTime timestamp)
            {
                return new HotkeyData
                {
                    Hotkey = new HotkeyGesture(args.KeyCode.ToInputKey(), args.Modifiers.ToModifiers()),
                    KeyEventArgs = args,
                    Timestamp = timestamp
                };
            }
        }
    }
}