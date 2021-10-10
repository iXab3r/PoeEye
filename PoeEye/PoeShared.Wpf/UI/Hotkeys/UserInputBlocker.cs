using System;
using System.Collections.Concurrent;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Services;
using WindowsHook;

namespace PoeShared.UI
{
    internal sealed class UserInputBlocker : DisposableReactiveObject, IUserInputBlocker
    {
        private static readonly IFluentLog Log = typeof(UserInputBlocker).PrepareLogger();
        private readonly ISharedResourceLatch allInputLatch;
        private readonly ISharedResourceLatch keyboardLatch;
        private readonly ISharedResourceLatch mouseLatch;
        private readonly ISharedResourceLatch mouseMoveLatch;
        private readonly ConcurrentDictionary<HotkeyGesture, EventArgs> pressedKeys = new();

        public UserInputBlocker(
            IKeyboardEventsSource keyboardEventsSource,
            ISharedResourceLatch allInputLatch,
            ISharedResourceLatch mouseMoveLatch,
            ISharedResourceLatch mouseLatch,
            ISharedResourceLatch keyboardLatch)
        {
            this.mouseMoveLatch = mouseMoveLatch.AddTo(Anchors);
            this.allInputLatch = allInputLatch.AddTo(Anchors);
            this.mouseLatch = mouseLatch.AddTo(Anchors);
            this.keyboardLatch = keyboardLatch.AddTo(Anchors);

            keyboardEventsSource.WhenKeyRaw.SubscribeSafe(HandleKeyboard, Log.HandleUiException).AddTo(Anchors);
            keyboardEventsSource.WhenMouseRaw.SubscribeSafe(HandleMouse, Log.HandleUiException).AddTo(Anchors);
        }

        public IDisposable Block(UserInputBlockType userInputBlockType)
        {
            var latch = userInputBlockType switch
            {
                UserInputBlockType.All => allInputLatch,
                UserInputBlockType.Keyboard => keyboardLatch,
                UserInputBlockType.Mouse => mouseLatch,
                UserInputBlockType.MouseMove => mouseMoveLatch,
                _ => throw new ArgumentOutOfRangeException(nameof(userInputBlockType), userInputBlockType, $"Unsupported user input block type: {userInputBlockType}")
            };

            return latch.Rent();
        }

        private void HandleMouse(MouseEventExtArgs eventArgs)
        {
            if (eventArgs.IsInjected || eventArgs.Handled)
            {
                return;
            }

            var gesture = eventArgs.ToGesture();

            var blockMouseButtons = allInputLatch.IsBusy || mouseLatch.IsBusy;
            var blockMouseMove = allInputLatch.IsBusy || mouseLatch.IsBusy || mouseMoveLatch.IsBusy;
            if (eventArgs.IsMouseButtonUp)
            {
                if (!pressedKeys.TryRemove(gesture, out _) || !blockMouseButtons)
                {
                    return;
                }

                Log.Debug(() => $"Suppressing physical mouse up: {eventArgs}");
                eventArgs.Handled = true;
                return;
            }

           
            if (eventArgs.IsMouseButtonDown)
            {
                if (!blockMouseButtons)
                {
                    return;
                }
                
                pressedKeys[gesture] = eventArgs;
                Log.Debug(() => $"Suppressing physical mouse down: {eventArgs}");
                eventArgs.Handled = true;
                return;
            }
            
             
            if (eventArgs.Delta != 0)
            {
                if (!blockMouseButtons)
                {
                    return;
                }
                Log.Debug(() => $"Suppressing physical mouse wheel: {eventArgs}");
                eventArgs.Handled = true;
                return;
            }

            if (!blockMouseMove)
            {
                return;
            }
            
            Log.Debug(() => $"Suppressing physical mouse event: {eventArgs}");
            eventArgs.Handled = true;
        }

        private void HandleKeyboard(KeyEventArgsExt eventArgs)
        {
            if (eventArgs.IsInjected || eventArgs.Handled)
            {
                return;
            }

            var gesture = eventArgs.ToGesture();

            var blockKeyboard = allInputLatch.IsBusy || keyboardLatch.IsBusy;
            if (eventArgs.IsKeyDown)
            {
                if (!blockKeyboard)
                {
                    return;
                }
                pressedKeys[gesture] = eventArgs;

                Log.Debug(() => $"Suppressing physical key down: {eventArgs}");
                eventArgs.Handled = true;
                return;
            }

            if (eventArgs.IsKeyUp)
            {
                if (!pressedKeys.TryRemove(gesture, out _) || !blockKeyboard)
                {
                    return;
                }

                Log.Debug(() => $"Suppressing physical key up: {eventArgs}");
                eventArgs.Handled = true;
                return;
            }

            if (!blockKeyboard)
            {
                return;
            }
            
            Log.Debug(() => $"Suppressing physical keyboard event: {eventArgs}");
            eventArgs.Handled = true;
        }
    }
}