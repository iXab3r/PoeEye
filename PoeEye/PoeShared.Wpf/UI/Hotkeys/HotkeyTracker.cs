using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData;
using Gma.System.MouseKeyHook;
using log4net;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Wpf.Services;
using PropertyBinder;
using ReactiveUI;
using Unity;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace PoeShared.UI
{
    internal sealed class HotkeyTracker : DisposableReactiveObject, IHotkeyTracker
    {
        private static readonly IFluentLog Log = typeof(HotkeyTracker).PrepareLogger();
        private static readonly Binder<HotkeyTracker> Binder = new();

        private readonly SourceCache<HotkeyGesture, HotkeyGesture> hotkeysSource = new(x => x);
        private readonly IClock clock;
        private readonly IAppArguments appArguments;
        private readonly IUserInputFilterConfigurator userInputFilterConfigurator;
        private readonly ISubject<HotkeyData> hotkeyLog = new Subject<HotkeyData>();
        private readonly ISet<HotkeyGesture> pressedKeys = new HashSet<HotkeyGesture>();

        private HotkeyGesture hotkey;
        private HotkeyMode hotkeyMode;
        private bool suppressKey;
        private bool isActive;
        private bool handleApplicationKeys;
        private bool ignoreModifiers;
        private bool hasModifiers;
        private bool isEnabled = true;

        static HotkeyTracker()
        {
            Binder.Bind(x => x.Hotkeys.Any(x => x.ModifierKeys != ModifierKeys.None)).To(x => x.HasModifiers);
            Binder.BindIf(x => x.HasModifiers, x => false).To(x => x.IgnoreModifiers);
            Binder.BindIf(x => x.Hotkeys.Any(x => x.IsMouse), x => false).To(x => x.SuppressKey);
        }

        public HotkeyTracker(
            IClock clock,
            IAppArguments appArguments,
            ISchedulerProvider schedulerProvider,
            IKeyboardEventsSource eventSource,
            IUserInputFilterConfigurator userInputFilterConfigurator,
            [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker)
        {
            var scheduler = schedulerProvider.GetOrCreate(nameof(HotkeyTracker));
            this.clock = clock;
            this.appArguments = appArguments;
            this.userInputFilterConfigurator = userInputFilterConfigurator;
            
            Disposable
                .Create(() => Log.Debug($"Disposing HotkeyTracker {this}"))
                .AddTo(Anchors);

            hotkeysSource
                .Connect()
                .Bind(out var hotkeys)
                .SubscribeToErrors(Log.HandleException)
                .AddTo(Anchors);
            Hotkeys = hotkeys;

            this.WhenAnyValue(x => x.Hotkey)
                .WithPrevious()
                .SubscribeSafe(x =>
                {
                    if (x.Previous != null)
                    {
                        Remove(x.Previous);
                    }

                    if (x.Current != null)
                    {
                        Add(x.Current);
                    }
                }, Log.HandleUiException)
                .AddTo(Anchors);

            hotkeyLog
                .Synchronize()
                .Where(x => Log.IsDebugEnabled && appArguments.IsDebugMode)
                .Select(data => $"Hotkey {(data.KeyDown ? "pressed" : "released")}: {data.Hotkey}, key: {data.Hotkey.Key}, mouse: {data.Hotkey.MouseButton}, wheel: {data.Hotkey.MouseWheel}, modifiers: {data.Hotkey.ModifierKeys}")
                .DistinctUntilChanged()
                .SubscribeSafe(data => Log.Debug(data), Log.HandleException)
                .AddTo(Anchors);

            Observable.CombineLatest(
                    hotkeysSource.Connect().Select(x => hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(", ")),
                    this.WhenAnyValue(x => x.HotkeyMode),
                    this.WhenAnyValue(x => x.IsEnabled),
                    (hotkeys, mode, isEnabled) => new {Hotkeys = hotkeys, HotkeyMode = mode, IsEnabled = isEnabled})
                .DistinctUntilChanged()
                .WithPrevious()
                .SubscribeSafe(
                    x =>
                    {
                        Log.Debug($"Tracking hotkey changed (hotkey gesture {x.Previous} => {x.Current})");
                        IsActive = false;
                    }, Log.HandleUiException)
                .AddTo(Anchors);

            Observable.CombineLatest(
                hotkeysSource.Connect(),
                this.WhenAnyValue(x => x.IsEnabled),
                (_, isEnabled) => new {Hotkeys = hotkeysSource.Items.ToArray(), IsEnabled = isEnabled})
                .Select(x => x.Hotkeys.Length > 0 && x.IsEnabled)
                .ObserveOn(scheduler)
                .Select(shouldSubscribe => !shouldSubscribe ? Observable.Empty<HotkeyData>() : BuildHotkeySubscription(eventSource))
                .Switch()
                .DistinctUntilChanged(x => new {x.Hotkey, x.KeyDown, x.Timestamp}) // removed possible duplicates from multiple sources
                .Where(
                    hotkeyData =>
                    {
                        /*
                         * This method MUST be executed on the same thread which emitted Key/Mouse event
                         * otherwise .Handled value will be ignored due to obvious concurrency reasons
                         */
                        
                        var mainWindowIsActive = mainWindowTracker.ActiveProcessId == mainWindowTracker.ExecutingProcessId;
                        if (mainWindowIsActive && !handleApplicationKeys)
                        {
                            if (HotkeyMode == HotkeyMode.Click)
                            {
                                Log.Debug($"[{this}] Skipping hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                                return false;
                            }

                            Log.Debug($"[{this}] Processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        }
                        else
                        {
                            Log.Debug($"[{this}] Application is NOT active, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        }

                        if (suppressKey)
                        {
                            if (KeyToModifier(hotkeyData.Hotkey.Key) != ModifierKeys.None)
                            {
                                Log.Debug($"Supplied key is a modifier, skipping suppression");
                            }
                            else
                            {
                                Log.Debug($"Marking hotkey {hotkeyData.Hotkey} as handled");
                                hotkeyData.MarkAsHandled();
                            }
                        }

                        return true;
                    })
                .DistinctUntilChanged(x => new {x.Hotkey, x.KeyDown}) // disables "blinking" when hotkey is held
                .WithPrevious()
                .ObserveOn(scheduler)
                .SubscribeSafe(
                    hotkeysPair =>
                    {
                        var sameHotkey = hotkeysPair.Previous.Hotkey?.Equals(hotkeysPair.Current.Hotkey) ?? false;
                        var sameState = hotkeysPair.Current.KeyDown == hotkeysPair.Previous.KeyDown;
                        var isMouseWheelEvent = (hotkeysPair.Current.Hotkey?.MouseWheel ?? MouseWheelAction.None) != MouseWheelAction.None;
                        
                        if (sameHotkey && sameState && !isMouseWheelEvent)
                        {
                            return;
                        }

                        var hotkeyData = hotkeysPair.Current;
                        Log.Debug($"[{this}] Hotkey {hotkeyData.Hotkey} pressed(isMouseWheel: {isMouseWheelEvent}), state: {(hotkeyData.KeyDown ? "down" : "up")}, suppressed: {suppressKey}, ignoreModifiers: {ignoreModifiers}");

                        if (isMouseWheelEvent)
                        {
                            if (HotkeyMode == HotkeyMode.Click)
                            {
                                IsActive = !IsActive;
                            }
                            else
                            {
                                IsActive = true;
                                IsActive = false;
                            }
                        }
                        else
                        {
                            if (HotkeyMode == HotkeyMode.Click)
                            {
                                if (!hotkeyData.KeyDown)
                                {
                                    IsActive = !IsActive;
                                }
                            }
                            else
                            {
                                IsActive = hotkeyData.KeyDown;
                            }
                        }
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);
            
            Binder.Attach(this).AddTo(Anchors);
        }

        public bool IsActive
        {
            get => isActive;
            private set => this.RaiseAndSetIfChanged(ref isActive, value);
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

        public bool IsEnabled
        {
            get => isEnabled;
            set => RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public bool IgnoreModifiers
        {
            get => ignoreModifiers;
            set => RaiseAndSetIfChanged(ref ignoreModifiers, value);
        }

        public bool HasModifiers
        {
            get => hasModifiers;
            private set => RaiseAndSetIfChanged(ref hasModifiers, value);
        }

        public bool SuppressKey
        {
            get => suppressKey;
            set => RaiseAndSetIfChanged(ref suppressKey, value);
        }

        public bool HandleApplicationKeys
        {
            get => handleApplicationKeys;
            set => RaiseAndSetIfChanged(ref handleApplicationKeys, value);
        }

        public void Add(HotkeyGesture hotkeyToAdd)
        {
            Log.Debug($"Registering hotkey {hotkeyToAdd}, tracker: {this}");
            hotkeysSource.AddOrUpdate(hotkeyToAdd);
        }

        public void Remove(HotkeyGesture hotkeyToRemove)
        {
            Log.Debug($"Unregistering hotkey {hotkeyToRemove}, tracker: {this}");
            hotkeysSource.RemoveKey(hotkeyToRemove);
        }

        public void Clear()
        {
            Log.Debug($"Unregistering all hotkeys, tracker: {this}");
            Hotkey = HotkeyGesture.Empty;
            hotkeysSource.Clear();
        }

        private bool IsConfiguredHotkey(HotkeyData data)
        {
            if (Log.IsDebugEnabled && appArguments.IsDebugMode)
            {
                hotkeyLog.OnNext(data);
            }
            
            if (data.Hotkey == null || data.Hotkey.IsEmpty)
            {
                // should never happen, hotkey data always contains something
                return false;
            }
            
            if (userInputFilterConfigurator.IsInWhitelist(data.Hotkey))
            {
                return false;
            }

            var isExactMatch = hotkeysSource.Items.Any(x => HotkeysAreEqual(x, data.Hotkey, ignoreModifiers));
            if (isExactMatch)
            {
                if (data.KeyDown)
                {
                    pressedKeys.Add(data.Hotkey);
                }
                else
                {
                    pressedKeys.Remove(data.Hotkey);
                }

                return true;
            }

            if (data.KeyDown || data.Hotkey.IsMouse || pressedKeys.Count == 0)
            {
                return false;
            }

            var keyAsModifier = KeyToModifier(data.Hotkey.Key);
            if (keyAsModifier == ModifierKeys.None)
            {
                // released key is not Modifier - not interested
                return false;
            }

            var pressed = pressedKeys.Where(x => x.ModifierKeys.HasFlag(keyAsModifier)).ToArray();
            if (pressed.Length == 0)
            {
                // released key was NOT detected before release
                return false;
            }

            if (pressed.Length > 1)
            {
                Log.Warn($"Probably something went wrong - there shouldn't be 2+ pressed hotkeys at once, pressed hotkeys: {pressed.DumpToString()}");
                return false;
            }

            // if user releases one of modifiers we simulate "release" of the button itself
            var newData = data.ReplaceKey(pressed[0]);
            Log.Debug($"Replaced hotkey {data} => {newData}");
            return IsConfiguredHotkey(newData);
        }

        private IObservable<HotkeyData> BuildHotkeySubscription(
            IKeyboardEventsSource eventSource)
        {
            if (hotkeysSource.Count == 0)
            {
                Log.Debug($"[{this}] Hotkey is not set");
                return Observable.Empty<HotkeyData>();
            }

            var result = new List<IObservable<HotkeyData>>();
            if (hotkeysSource.Items.Any(x => x.IsKeyboard))
            {
                Log.Debug($"[{this}] Subscribing to Keyboard events");
                eventSource.WhenKeyDown.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(true))
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);

                eventSource.WhenKeyUp.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (hotkeysSource.Items.Any(x => x.IsMouse))
            {
                Log.Debug($"[{this}] Subscribing to Mouse events");
                eventSource.WhenMouseDown.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(true))
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);

                eventSource.WhenMouseUp.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (hotkeysSource.Items.Any(x => x.IsMouseWheel))
            {
                Log.Debug($"[{this}] Subscribing to Mouse Wheel events");
                eventSource.WhenMouseWheel.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (result.Count == 0)
            {
                Log.Debug($"[{this}] Could not find correct subscription for hotkeys, tracker: {this}");
                return Observable.Empty<HotkeyData>();
            }

            return result.Merge();
        }

        public override string ToString()
        {
            return $"HotkeyTracker {hotkeyMode} {hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(" OR ")}";
        }

        private static bool HotkeysAreEqual(HotkeyGesture key1, HotkeyGesture key2, bool ignoreModifiers)
        {
            return !ignoreModifiers ? key1.ToString().Equals(key2.ToString()) : ExtractKeyWithoutModifiers(key1).Equals(ExtractKeyWithoutModifiers(key2));
        }

        private static string ExtractKeyWithoutModifiers(HotkeyGesture key)
        {
            if (key.MouseButton != null)
            {
                return key.MouseButton.ToString();
            } else if (key.Key != Key.None)
            {
                return key.Key.ToString();
            } else if (key.MouseWheel != MouseWheelAction.None)
            {
                return key.MouseWheel.ToString();
            }
            return string.Empty;
        }

        private static ModifierKeys KeyToModifier(Key key)
        {
            return key switch
            {
                Key.LeftAlt => ModifierKeys.Alt,
                Key.RightAlt => ModifierKeys.Alt,
                Key.LeftCtrl => ModifierKeys.Control,
                Key.RightCtrl => ModifierKeys.Control,
                Key.LeftShift => ModifierKeys.Shift,
                Key.RightShift => ModifierKeys.Shift,
                Key.LWin => ModifierKeys.Windows,
                Key.RWin => ModifierKeys.Windows,
                _ => ModifierKeys.None
            };
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

            public HotkeyData ReplaceKey(HotkeyGesture newHotkey)
            {
                Hotkey = newHotkey;
                return this;
            }

            public HotkeyData SetTimestamp(DateTime value)
            {
                Timestamp = value;
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

            public static HotkeyData FromEvent(MouseEventArgs args, IClock clock)
            {
                var modifiers = UnsafeNative.GetCurrentModifierKeys();
                return new HotkeyData
                {
                    Hotkey = args.Delta != 0 ? new HotkeyGesture(args.Delta > 0 ? MouseWheelAction.WheelUp : MouseWheelAction.WheelDown, modifiers) : new HotkeyGesture(args.Button, modifiers),
                    MouseEventArgs = args,
                    Timestamp = clock.UtcNow
                };
            }

            public static HotkeyData FromEvent(KeyEventArgs args, IClock clock)
            {
                return new()
                {
                    Hotkey = new HotkeyGesture(args.KeyCode.ToInputKey(), args.Modifiers.ToModifiers()),
                    KeyEventArgs = args,
                    Timestamp = clock.UtcNow
                };
            }
        }
    }
}