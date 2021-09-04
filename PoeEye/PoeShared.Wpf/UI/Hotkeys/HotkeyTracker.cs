using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData;
using WindowsHook;
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
        private static readonly Binder<HotkeyTracker> Binder = new();
        private readonly IAppArguments appArguments;
        private readonly IClock clock;
        private readonly ISubject<HotkeyData> hotkeyLog = new Subject<HotkeyData>();

        private readonly SourceCache<HotkeyGesture, HotkeyGesture> hotkeysSource = new(x => x);
        private readonly ISet<HotkeyGesture> pressedKeys = new HashSet<HotkeyGesture>();
        private readonly IScheduler uiScheduler;
        private readonly IUserInputFilterConfigurator userInputFilterConfigurator;
        private bool canSuppressHotkey;
        private bool handleApplicationKeys;
        private bool hasModifiers;

        private HotkeyGesture hotkey;
        private HotkeyMode hotkeyMode;
        private bool ignoreModifiers;
        private bool isActive;
        private bool isEnabled = true;
        private bool suppressKey;

        static HotkeyTracker()
        {
            Binder.Bind(x => x.Hotkeys.All(CanBeSuppressed)).To(x => x.CanSuppressHotkey);
            Binder.Bind(x => x.Hotkeys.Any(x => x.ModifierKeys != ModifierKeys.None)).To(x => x.HasModifiers);
            Binder.BindIf(x => x.HasModifiers, x => false).To(x => x.IgnoreModifiers);
            Binder
                .BindIf(x => x.CanSuppressHotkey == false, x => false)
                .To((x,v) => x.SuppressKey = v, x => x.uiScheduler);
        }

        public HotkeyTracker(
            IClock clock,
            IAppArguments appArguments,
            ISchedulerProvider schedulerProvider,
            IKeyboardEventsSource eventSource,
            IUserInputFilterConfigurator userInputFilterConfigurator,
            [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Log = typeof(HotkeyTracker).PrepareLogger().WithSuffix(this);
            var scheduler = schedulerProvider.GetOrCreate("Hotkey");
            this.clock = clock;
            this.appArguments = appArguments;
            this.userInputFilterConfigurator = userInputFilterConfigurator;
            this.uiScheduler = uiScheduler;

            Disposable
                .Create(() => Log.Debug($"Disposing HotkeyTracker"))
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
                .Where(x => Log.IsDebugEnabled && appArguments.IsDebugMode)
                .Select(data => $"Hotkey {(data.KeyDown ? "pressed" : "released")}: {data.Hotkey}, key: {data.Hotkey.Key}, mouse: {data.Hotkey.MouseButton}, wheel: {data.Hotkey.MouseWheel}, modifiers: {data.Hotkey.ModifierKeys}")
                .DistinctUntilChanged()
                .SubscribeSafe(data => Log.Debug(data), Log.HandleException)
                .AddTo(Anchors);

            Observable.CombineLatest(
                    hotkeysSource.Connect().Select(x => hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(", ")),
                    this.WhenAnyValue(x => x.HotkeyMode),
                    (hotkeys, mode) => new {Hotkeys = hotkeys, HotkeyMode = mode})
                .DistinctUntilChanged()
                .WithPrevious()
                .SubscribeSafe(
                    x =>
                    {
                        Log.Debug($"Tracking hotkey changed, {x.Previous} => {x.Current}");
                        Reset();
                    }, Log.HandleUiException)
                .AddTo(Anchors);
            
            Observable.CombineLatest(
                hotkeysSource.Connect(),
                this.WhenAnyValue(x => x.IsEnabled),
                (_, isEnabled) => new {Hotkeys = hotkeysSource.Items.ToArray(), IsEnabled = isEnabled})
                .Select(x => x.Hotkeys.Length > 0 && x.IsEnabled)
                .ObserveOn(scheduler)
                .Select(shouldSubscribe => !shouldSubscribe ? Observable.Return(default(HotkeyData)) : BuildHotkeySubscription(eventSource))
                .Switch()
                .DistinctUntilChanged(x => new {x?.Hotkey, x?.KeyDown, x?.Timestamp}) // removed possible duplicates from multiple sources
                .Where(
                    hotkeyData =>
                    {
                        /*
                         * This method MUST be executed on the same thread which emitted Key/Mouse event
                         * otherwise .Handled value will be ignored due to obvious concurrency reasons
                         */

                        if (hotkeyData == null)
                        {
                            Log.Debug("Received empty hotkey data event, propagating it");
                            return true;
                        }
                        
                        var mainWindowIsActive = mainWindowTracker.ActiveProcessId == mainWindowTracker.ExecutingProcessId;
                        if (mainWindowIsActive)
                        {
                            if (!handleApplicationKeys)
                            {
                                if (HotkeyMode == HotkeyMode.Click)
                                {
                                    Log.Debug($"Skipping hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                                    return false;
                                }

                                Log.Debug($"Application is active, but mode is {hotkeyMode}, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                            }
                            else
                            {
                                Log.Debug($"Application is active, but {nameof(HandleApplicationKeys)} is set to true, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                            }
                        }
                        else
                        {
                            Log.Debug($"Application is NOT active, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey}, ignoreModifiers: {ignoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        }

                        if (suppressKey)
                        {
                            if (KeyToModifier(hotkeyData.Hotkey.Key) != ModifierKeys.None)
                            {
                                Log.Debug($"Supplied key {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}) is a modifier, skipping suppression");
                            }
                            else
                            {
                                Log.Debug($"Marking hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}) as handled");
                                hotkeyData.MarkAsHandled();
                            }
                        }

                        return true;
                    })
                .WithPrevious()
                .ObserveOn(scheduler)
                .SubscribeSafe(
                    hotkeysPair =>
                    {
                        if (hotkeysPair.Current == null)
                        {
                            Log.Debug($"Skipping event, hotkey data is null: {hotkeysPair}");
                            return;
                        }
                        
                        var sameHotkey = hotkeysPair.Previous?.Hotkey?.Equals(hotkeysPair.Current?.Hotkey) ?? false;
                        var sameState = hotkeysPair.Current?.KeyDown == hotkeysPair.Previous?.KeyDown;
                        var isMouseWheelEvent = (hotkeysPair.Current?.Hotkey?.MouseWheel ?? MouseWheelAction.None) != MouseWheelAction.None;
                        
                        if (sameHotkey && sameState && !isMouseWheelEvent)
                        {
                            Log.Debug($"Skipping duplicate event: {hotkeysPair}");
                            return;
                        }

                        var hotkeyData = hotkeysPair.Current;
                        Log.Debug($"Updating tracker state, hotkey {hotkeyData.Hotkey} pressed(isMouseWheel: {isMouseWheelEvent}), state: {(hotkeyData.KeyDown ? "down" : "up")}, suppressed: {suppressKey}, ignoreModifiers: {ignoreModifiers}");

                        if (isMouseWheelEvent)
                        {
                            if (HotkeyMode == HotkeyMode.Click)
                            {
                                Log.Debug($"Toggling hotkey state for Wheel event");
                                IsActive = !IsActive;
                            }
                            else
                            {
                                Log.Debug($"Blinking hotkey state");
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
                                    Log.Debug($"Toggling hotkey state");
                                    IsActive = !IsActive;
                                }
                            }
                            else
                            {
                                Log.Debug($"Setting state to KeyDown: {hotkeyData.KeyDown}");
                                IsActive = hotkeyData.KeyDown;
                            }
                        }
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsActive)
                .WithPrevious()
                .SubscribeSafe(x => Log.Debug($"Hotkey state changed: {x}"), Log.HandleException)
                .AddTo(Anchors);
            
            Binder.Attach(this).AddTo(Anchors);
        }

        private IFluentLog Log { get; }

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

        public bool CanSuppressHotkey
        {
            get => canSuppressHotkey;
            private set => RaiseAndSetIfChanged(ref canSuppressHotkey, value);
        }

        public bool HandleApplicationKeys
        {
            get => handleApplicationKeys;
            set => RaiseAndSetIfChanged(ref handleApplicationKeys, value);
        }

        public void Add(HotkeyGesture hotkeyToAdd)
        {
            Log.Debug($"Registering hotkey {hotkeyToAdd}");
            hotkeysSource.AddOrUpdate(hotkeyToAdd);
        }

        public void Remove(HotkeyGesture hotkeyToRemove)
        {
            Log.Debug($"Unregistering hotkey {hotkeyToRemove}");
            hotkeysSource.RemoveKey(hotkeyToRemove);
        }

        public void Clear()
        {
            Log.Debug($"Unregistering all hotkeys");
            Hotkey = HotkeyGesture.Empty;
            hotkeysSource.Clear();
        }

        public void Reset()
        {
            Log.Debug("Resetting state");
            IsActive = false;
        }

        private static bool CanBeSuppressed(HotkeyGesture hotkey)
        {
            return hotkey.IsKeyboard || hotkey.IsMouseButton && hotkey.MouseButton != MouseButton.Left && hotkey.MouseButton != MouseButton.Right;
        }

        private bool IsConfiguredHotkey(HotkeyData data)
        {
            if (data.Hotkey == null || data.Hotkey.IsEmpty)
            {
                // should never happen, hotkey data always contains something
                return false;
            }
            
            if (userInputFilterConfigurator.IsInWhitelist(data.Hotkey))
            {
                Log.Debug($"Pressed hotkey {data} is in whitelist, skipping it");
                return false;
            }

            var isMatch = hotkeysSource.Items.Any(x => HotkeysAreEqual(x, data.Hotkey, ignoreModifiers || hotkeyMode == HotkeyMode.Hold && !data.KeyDown));
            if (isMatch)
            {
                if (data.KeyDown)
                {
                    Log.Debug($"Adding hotkey {data.Hotkey} to pressed keys");
                    pressedKeys.Add(data.Hotkey);
                }
                else
                {
                    var isPressed = pressedKeys.FirstOrDefault(x => HotkeysAreEqual(x, data.Hotkey, true));
                    if (isPressed == null && !data.Hotkey.IsMouseWheel)
                    {
                        Log.Debug($"Released hotkey {data.Hotkey} is not in pressed keys or is not a mouse wheel event list skipping it");
                        return false;
                    }
                    else
                    {
                        Log.Debug($"Removing released hotkey {data.Hotkey} from pressed keys");
                        pressedKeys.Remove(isPressed);
                    }
                }

                Log.Debug($"Processing matching hotkey {data}");
                return true;
            }
            
            if (data.KeyDown)
            {
                Log.Debug($"Skipping key down event for {data} - not a match");
                return false;
            }
            
            if (data.Hotkey.IsMouse)
            {
                Log.Debug($"Pressed hotkey {data} is mouse key, skipping it");
                return false;
            }

            if (pressedKeys.Count == 0)
            {
                Log.Debug($"Pressed hotkeys list is empty, skipping hotkey {data}");
                return false;
            }

            var keyAsModifier = KeyToModifier(data.Hotkey.Key);
            if (keyAsModifier == ModifierKeys.None)
            {
                // released key is not Modifier - not interested
                Log.Debug($"Pressed hotkey {data} is not a modifier, skipping it");
                return false;
            }

            var pressed = pressedKeys.Where(x => x.ModifierKeys.HasFlag(keyAsModifier)).ToArray();
            if (pressed.Length == 0)
            {
                // released key was NOT detected before release
                Log.Debug($"There are no pressed keys with modifier {keyAsModifier}");
                return false;
            }

            if (pressed.Length > 1)
            {
                Log.Warn($"Probably something went wrong - there shouldn't be 2+ pressed hotkeys at once, pressed hotkeys: {pressed.DumpToString()}");
                return false;
            }

            // if user releases one of modifiers we simulate "release" of the button itself
            var newHotkey = pressed[0];
            Log.Debug($"Replacing hotkey {data.Hotkey} => {newHotkey} in {data}");
            return IsConfiguredHotkey(data with { Hotkey = newHotkey});
        }

        private IObservable<HotkeyData> BuildHotkeySubscription(
            IKeyboardEventsSource eventSource)
        {
            if (hotkeysSource.Count == 0)
            {
                Log.Debug($"Hotkey is not set");
                return Observable.Empty<HotkeyData>();
            }

            var result = new List<IObservable<HotkeyData>>();
            if (hotkeysSource.Items.Any(x => x.IsKeyboard))
            {
                Log.Debug($"Subscribing to Keyboard events");
                eventSource.WhenKeyDown.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(true))
                    .Do(LogHotkey)
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);

                eventSource.WhenKeyUp.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Do(LogHotkey)
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (hotkeysSource.Items.Any(x => x.IsMouse))
            {
                Log.Debug($"Subscribing to Mouse events");
                eventSource.WhenMouseDown.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(true))
                    .Do(LogHotkey)
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);

                eventSource.WhenMouseUp.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Do(LogHotkey)
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (hotkeysSource.Items.Any(x => x.IsMouseWheel))
            {
                Log.Debug($"Subscribing to Mouse Wheel events");
                eventSource.WhenMouseWheel.Select(x => HotkeyData.FromEvent(x, clock))
                    .Select(x => x.SetKeyDown(false))
                    .Do(LogHotkey)
                    .Where(IsConfiguredHotkey)
                    .AddTo(result);
            }

            if (result.Count == 0)
            {
                Log.Debug($"Could not find correct subscription for hotkeys");
                return Observable.Empty<HotkeyData>();
            }

            return result.Merge();
        }

        private void LogHotkey(HotkeyData data)
        {
            if (Log.IsDebugEnabled && appArguments.IsDebugMode)
            {
                hotkeyLog.OnNext(data);
            }
        }

        public override string ToString()
        {
            return $"{(IsEnabled ? default : "DISABLED ")}Hotkey {hotkeyMode} {hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(" OR ")}{(IsActive ? " Active" : default)}";
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

        private sealed record HotkeyData
        {
            public KeyEventArgs KeyEventArgs { get; init; }

            public MouseEventArgs MouseEventArgs { get; init; }

            public HotkeyGesture Hotkey { get; set; }

            public bool KeyDown { get; set; }

            public DateTime Timestamp { get; set; }

            public HotkeyData SetKeyDown(bool value)
            {
                KeyDown = value;
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