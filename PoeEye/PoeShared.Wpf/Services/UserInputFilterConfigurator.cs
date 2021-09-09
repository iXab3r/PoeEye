using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.UI;
using WindowsHook;

namespace PoeShared.Services
{
    internal sealed class UserInputFilterConfigurator : DisposableReactiveObject, IUserInputFilterConfigurator, IMouseEventFilter, IKeyboardEventFilter
    {
        private static readonly IFluentLog Log = typeof(UserInputFilterConfigurator).PrepareLogger();
        
        /// <summary>
        ///   Items removed from whitelist will still be ignored for this period of time. It gives some time for OS to process inputs that were emulated
        /// </summary>
        private static readonly TimeSpan AllowedTimeframeForWhitelistedItems = TimeSpan.FromMilliseconds(10);

        private readonly IAppArguments appArguments;
        private readonly Dictionary<HotkeyGesture, GestureState> keyboardState = new(new LambdaComparer<HotkeyGesture>((first, second) => first?.Equals(second, ignoreModifiers: true) ?? false));

        public UserInputFilterConfigurator(
            IAppArguments appArguments,
            IKeyboardEventsSource keyboardEventsSource)
        {
            this.appArguments = appArguments;
            keyboardEventsSource.AddKeyboardFilter(this).AddTo(Anchors);
            keyboardEventsSource.AddMouseFilter(this).AddTo(Anchors);
        }

        public bool ShouldProcess(KeyEventArgsExt eventArgs)
        {
            var hotkey = new HotkeyGesture(eventArgs.KeyCode.ToInputKey(), eventArgs.Modifiers.ToModifiers());
            return ShouldProcess(hotkey, eventArgs.IsKeyDown);
        }

        public bool ShouldProcess(MouseEventExtArgs eventArgs)
        {
            var modifiers = eventArgs.Modifiers.ToModifiers();
            var hotkey = eventArgs.Delta != 0 ? new HotkeyGesture(eventArgs.Delta > 0 ? MouseWheelAction.WheelUp : MouseWheelAction.WheelDown, modifiers) : new HotkeyGesture(eventArgs.Button, modifiers);
            return ShouldProcess(hotkey, eventArgs.IsMouseButtonDown);
        }

        public IDisposable AddToWhitelist([NotNull] HotkeyGesture hotkey)
        {
            if (hotkey == null)
            {
                throw new ArgumentNullException(nameof(hotkey));
            }

            lock (keyboardState)
            {
                if (keyboardState.TryGetValue(hotkey, out var state))
                {
                    Log.Debug($"Incrementing usages of hotkey {hotkey} in state {state} {state.WhitelistRefCount} => {state.WhitelistRefCount + 1}");
                    state.IncrementRefCount();
                }
                else
                {
                    Log.Debug($"Adding {hotkey} to whitelist");
                    keyboardState[hotkey] = new GestureState(hotkey);
                }
            }

            return Disposable.Create(() =>
            {
                lock (keyboardState)
                {
                    if (!keyboardState.TryGetValue(hotkey, out var state))
                    {
                        throw new ApplicationException($"Failed to release hotkey {hotkey} from whitelist");
                    }
                    Log.Debug($"Decrementing usages of hotkey {hotkey} {state.WhitelistRefCount} => {state.WhitelistRefCount - 1}");
                    state.DecrementRefCount();
                }
            });
        }

        private bool ShouldProcess(HotkeyGesture hotkey, bool isKeyDown)
        {
            if (Log.IsDebugEnabled && appArguments.IsDebugMode)
            {
                Log.Debug($"Hotkey {(isKeyDown ? "pressed" : "released")}: {hotkey}, key: {hotkey.Key}, mouse: {hotkey.MouseButton}, wheel: {hotkey.MouseWheel}, modifiers: {hotkey.ModifierKeys}");
            }
            
            lock (keyboardState)
            {
                if (!keyboardState.TryGetValue(hotkey, out var state))
                {
                    return true;
                }

                if (state.IsWhitelisted)
                {
                    Log.Info($"Ignoring whitelisted hotkey {hotkey}, state: {state}");
                    return false;
                }

                if (state.TimeSinceExclusionFromWhitelist < AllowedTimeframeForWhitelistedItems)
                {
                    Log.Info($"Ignoring hotkey {hotkey} - it is excluded from whitelist very recently and should still be ignored, state: {state}");
                    return false;
                }
            }
            return true;
        }

        private sealed record GestureState
        {
            public GestureState(HotkeyGesture gesture)
            {
                Gesture = gesture;
                IncrementRefCount();
            }

            public HotkeyGesture Gesture { get; }

            public int WhitelistRefCount { get; private set; }

            public bool IsWhitelisted => WhitelistRefCount > 0;

            public TimeSpan TimeSinceExclusionFromWhitelist => TimeSpan.FromSeconds(((double)Stopwatch.GetTimestamp() - ExclusionFromWhitelistTimestamp) / Stopwatch.Frequency);

            public long ExclusionFromWhitelistTimestamp { get; private set; }

            public void IncrementRefCount()
            {
                WhitelistRefCount++;
            }

            public void DecrementRefCount()
            {
                WhitelistRefCount--;
                if (WhitelistRefCount <= 0)
                {
                    ExclusionFromWhitelistTimestamp = Stopwatch.GetTimestamp();
                }
            }
        }
    }
}