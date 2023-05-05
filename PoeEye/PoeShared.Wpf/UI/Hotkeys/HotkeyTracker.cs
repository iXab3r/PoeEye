using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Hotkeys;
using WindowsHook;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;
using ReactiveUI;
using Unity;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace PoeShared.UI;

internal sealed class HotkeyTracker : DisposableReactiveObject, IHotkeyTracker
{
    private static readonly Binder<HotkeyTracker> Binder = new();
    private readonly IClock clock;

    private readonly SourceCache<HotkeyGesture, HotkeyGesture> hotkeysSource = new(x => x);
    private readonly ISet<HotkeyGesture> pressedKeys = new HashSet<HotkeyGesture>();

    static HotkeyTracker()
    {
        Binder.Bind(x => x.Hotkeys.All(CanBeSuppressed)).To(x => x.CanSuppressHotkey);
        Binder.Bind(x => x.Hotkeys.Any(x => x.ModifierKeys != ModifierKeys.None)).To(x => x.HasModifiers);
        Binder.BindIf(x => x.HasModifiers, x => false).To(x => x.IgnoreModifiers);
        Binder
            .BindIf(x => x.CanSuppressHotkey == false, x => false)
            .To((x,v) => x.SuppressKey = v);
    }

    public HotkeyTracker(
        IClock clock,
        IKeyboardEventsSource eventSource,
        [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
        [Dependency(WellKnownSchedulers.InputHook)] IScheduler inputScheduler)
    {
        Log = typeof(HotkeyTracker).PrepareLogger().WithSuffix(this);
        this.clock = clock;

        Disposable
            .Create(() => Log.Debug(() => $"Disposing HotkeyTracker"))
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

        Observable.CombineLatest(
                hotkeysSource.Connect().Select(x => hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(", ")),
                this.WhenAnyValue(x => x.HotkeyMode),
                (hotkeys, mode) => new {Hotkeys = hotkeys, HotkeyMode = mode})
            .DistinctUntilChanged()
            .WithPrevious()
            .ObserveOn(inputScheduler)
            .SubscribeSafe(
                x =>
                {
                    Log.Debug(() => $"Tracking hotkey changed, {x.Previous} => {x.Current}");
                    Reset();
                }, Log.HandleUiException)
            .AddTo(Anchors);
            
        Observable.CombineLatest(
                hotkeysSource.Connect(),
                this.WhenAnyValue(x => x.IsEnabled),
                (_, isEnabled) => new {Hotkeys = hotkeysSource.Items.ToArray(), IsEnabled = isEnabled})
            .Select(x => x.Hotkeys.Length > 0 && x.IsEnabled)
            .ObserveOn(inputScheduler)
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
                        if (!HandleApplicationKeys)
                        {
                            if (HotkeyMode == HotkeyMode.Click)
                            {
                                Log.Debug(() => $"Skipping hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, SuppressKey: {SuppressKey}, IgnoreModifiers: {IgnoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                                return false;
                            }

                            Log.Debug(() => $"Application is active, but mode is {HotkeyMode}, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, SuppressKey: {SuppressKey}, IgnoreModifiers: {IgnoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        }
                        else
                        {
                            Log.Debug(() => $"Application is active, but {nameof(HandleApplicationKeys)} is set to true, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, SuppressKey: {SuppressKey}, IgnoreModifiers: {IgnoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        }
                    }
                    else
                    {
                        Log.Debug(() => $"Application is NOT active, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, SuppressKey: {SuppressKey}, IgnoreModifiers: {IgnoreModifiers}, configuredKey: {Hotkey}, mode: {HotkeyMode})");
                    }

                    if (SuppressKey)
                    {
                        if (KeyToModifier(hotkeyData.Hotkey.Key) != ModifierKeys.None)
                        {
                            Log.Debug(() => $"Supplied key {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}) is a modifier, skipping suppression");
                        }
                        else
                        {
                            Log.Debug(() => $"Marking hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}) as handled");
                            hotkeyData.MarkAsHandled();
                        }
                    }

                    if (hotkeyData.IsHandled)
                    {
                        Log.Debug(() => $"Hotkey is marked as handled - it will not be seen by the system {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown})");
                    }
                    else
                    {
                        Log.Debug(() => $"Hotkey is not marked as handled - it will be seen by the system {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown})");
                    }

                    return true;
                })
            .WithPrevious()
            .ObserveOn(inputScheduler)
            .SubscribeSafe(
                hotkeysPair =>
                {
                    if (hotkeysPair.Current == null)
                    {
                        Log.Debug(() => $"Skipping event, hotkey data is null: {hotkeysPair}");
                        return;
                    }        
                        
                    var sameHotkey = hotkeysPair.Previous?.Hotkey?.Equals(hotkeysPair.Current?.Hotkey) ?? false;
                    var sameState = hotkeysPair.Current?.KeyDown == hotkeysPair.Previous?.KeyDown;
                    var sameTimestamp = hotkeysPair.Current?.Timestamp == hotkeysPair.Previous?.Timestamp;
                    var isMouseWheelEvent = (hotkeysPair.Current?.Hotkey?.MouseWheel ?? MouseWheelAction.None) != MouseWheelAction.None;
                        
                    if (sameHotkey && sameState && sameTimestamp && !isMouseWheelEvent)
                    {
                        Log.Debug(() => $"Skipping duplicate event: {hotkeysPair}");
                        return;
                    }

                    var hotkeyData = hotkeysPair.Current;
                    Log.Debug(() => $"Updating tracker state, hotkey {hotkeyData.Hotkey} pressed(isMouseWheel: {isMouseWheelEvent}), state: {(hotkeyData.KeyDown ? "down" : "up")}, suppressed: {SuppressKey}, IgnoreModifiers: {IgnoreModifiers}");

                    if (isMouseWheelEvent)
                    {
                        if (HotkeyMode == HotkeyMode.Click)
                        {
                            Log.Debug(() => $"Toggling hotkey state for Wheel event");
                            IsActive = !IsActive;
                        }
                        else
                        {
                            Log.Debug(() => $"Blinking hotkey state");
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
                                Log.Debug(() => $"Toggling hotkey state");
                                IsActive = !IsActive;
                            }
                        }
                        else
                        {
                            Log.Debug(() => $"Setting state to KeyDown: {hotkeyData.KeyDown}");
                            IsActive = hotkeyData.KeyDown;
                        }
                    }
                },
                Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.IsActive)
            .WithPrevious()
            .SubscribeSafe(x => Log.Debug(() => $"Hotkey state changed: {x}"), Log.HandleException)
            .AddTo(Anchors);
            
        Binder.Attach(this).AddTo(Anchors);
    }

    private IFluentLog Log { get; }

    public bool IsActive { get; private set; }

    public HotkeyGesture Hotkey { get; set; }

    public ReadOnlyObservableCollection<HotkeyGesture> Hotkeys { get; }

    public HotkeyMode HotkeyMode { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IgnoreModifiers { get; set; }

    public bool HasModifiers { get; [UsedImplicitly] private set; }

    public bool SuppressKey { get; set; }

    public bool CanSuppressHotkey { get; [UsedImplicitly] private set; }

    public bool HandleApplicationKeys { get; set; }

    public void Add(params HotkeyGesture[] hotkeysToAdd)
    {
        Log.Debug(() => $"Registering hotkeys {hotkeysToAdd.DumpToString()}");
        hotkeysSource.Edit(list =>
        {
            hotkeysToAdd.ForEach(list.AddOrUpdate);
        });
    }

    public void Remove(params HotkeyGesture[] hotkeysToRemove)
    {
        Log.Debug(() => $"Unregistering hotkeys {hotkeysToRemove.DumpToString()}");
        hotkeysSource.Edit(list =>
        {
            hotkeysToRemove.ForEach(list.Remove);
        });
    }

    public void Clear()
    {
        Log.Debug(() => $"Unregistering all hotkeys");
        Hotkey = HotkeyGesture.Empty;
        hotkeysSource.Clear();
    }

    public void Reset()
    {
        Log.Debug("Resetting hotkey state");
        IsActive = false;
    }
    
    public void Activate()
    {
        Log.Debug("Activating hotkey");
        IsActive = false;
    }

    private static bool CanBeSuppressed(HotkeyGesture hotkey)
    {
        if (hotkey.IsEmpty)
        {
            return true;
        }
        return hotkey.IsKeyboard || hotkey.IsMouseButton && hotkey.MouseButton != MouseButton.Left && hotkey.MouseButton != MouseButton.Right;
    }

    private bool IsConfiguredHotkey(HotkeyData data)
    {
        if (data.Hotkey == null || data.Hotkey.IsEmpty)
        {
            // should never happen, hotkey data always contains something
            return false;
        }

        var isMatch = hotkeysSource.Items.Any(x => x.Equals(data.Hotkey, IgnoreModifiers || HotkeyMode == HotkeyMode.Hold && !data.KeyDown));
        if (isMatch)
        {
            if (data.IsHandled)
            {
                return false;
            }
                
            if (data.KeyDown)
            {
                Log.Debug(() => $"Adding hotkey {data.Hotkey} to pressed keys");
                pressedKeys.Add(data.Hotkey);
            }
            else
            {
                var isPressed = pressedKeys.FirstOrDefault(x => x.Equals(data.Hotkey, true));
                if (isPressed == null && !data.Hotkey.IsMouseWheel)
                {
                    Log.Debug(() => $"Released hotkey {data.Hotkey} is not in pressed keys or is not a mouse wheel event list skipping it");
                    return false;
                }
                else
                {
                    Log.Debug(() => $"Removing released hotkey {data.Hotkey} from pressed keys");
                    pressedKeys.Remove(isPressed);
                }
            }

            Log.Debug(() => $"Processing matching hotkey {data}");
            return true;
        }
            
        if (data.KeyDown)
        {
            return false;
        }
            
        if (data.Hotkey.IsMouse)
        {
            return false;
        }

        if (pressedKeys.Count == 0)
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
            Log.Warn(() => $"Probably something went wrong - there shouldn't be 2+ pressed hotkeys at once, pressed hotkeys: {pressed.DumpToString()}");
            return false;
        }

        if (IgnoreModifiers)
        {
            Log.Debug(() => $"Released modifier will be ignored: {data}");
            return false;
        }

        // if user releases one of modifiers we simulate "release" of the button itself
        var newHotkey = pressed[0];
        Log.Debug(() => $"Replacing hotkey {data.Hotkey} => {newHotkey} in {data}");
        return IsConfiguredHotkey(data with { Hotkey = newHotkey});
    }

    private IObservable<HotkeyData> BuildHotkeySubscription(
        IKeyboardEventsSource eventSource)
    {
        if (hotkeysSource.Count == 0)
        {
            Log.Debug(() => $"Hotkey is not set");
            return Observable.Empty<HotkeyData>();
        }

        var result = new List<IObservable<HotkeyData>>();
        if (hotkeysSource.Items.Any(x => x.IsKeyboard))
        {
            Log.Debug(() => $"Subscribing to Keyboard events");
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
            Log.Debug(() => $"Subscribing to Mouse events");
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
            Log.Debug(() => $"Subscribing to Mouse Wheel events");
            eventSource.WhenMouseWheel.Select(x => HotkeyData.FromEvent(x, clock))
                .Select(x => x.SetKeyDown(false))
                .Where(IsConfiguredHotkey)
                .AddTo(result);
        }

        if (result.Count == 0)
        {
            Log.Debug(() => $"Could not find correct subscription for hotkeys");
            return Observable.Empty<HotkeyData>();
        }

        return result.Merge();
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append($"{(IsEnabled ? default : "DISABLED ")}Hotkey");
        builder.AppendParameter(nameof(HotkeyMode), HotkeyMode);
        builder.AppendParameter("Keys", hotkeysSource.Items.Select(x => x.ToString()).JoinStrings(" OR "));
        builder.AppendParameter(nameof(IsActive), IsActive);
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
        private KeyEventArgs KeyEventArgs { get; init; }

        private MouseEventExtArgs MouseEventArgs { get; init; }

        public HotkeyGesture Hotkey { get; set; }

        public bool KeyDown { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsHandled
        {
            get
            {
                if (KeyEventArgs != null)
                {
                    return KeyEventArgs.Handled;
                }

                if (MouseEventArgs != null)
                {
                    return MouseEventArgs.Handled;
                }

                return false;
            }
        }

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

            if (MouseEventArgs != null)
            {
                MouseEventArgs.Handled = true;
            }

            return this;
        }

        public static HotkeyData FromEvent(MouseEventExtArgs args, IClock clock)
        {
            return new HotkeyData
            {
                Hotkey = args.ToGesture(),
                MouseEventArgs = args,
                Timestamp = clock.UtcNow
            };
        }

        public static HotkeyData FromEvent(KeyEventArgs args, IClock clock)
        {
            return new()
            {
                Hotkey = args.ToGesture(),
                KeyEventArgs = args,
                Timestamp = clock.UtcNow
            };
        }
    }
}