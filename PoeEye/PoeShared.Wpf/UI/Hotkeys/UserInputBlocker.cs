using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using WindowsHook;

namespace PoeShared.UI;

internal sealed class UserInputBlocker : DisposableReactiveObject, IUserInputBlocker
{
    private static readonly IFluentLog Log = typeof(UserInputBlocker).PrepareLogger();
    private readonly ISharedResourceLatch allInputLatch;
    private readonly ISharedResourceLatch keyboardLatch;
    private readonly ISharedResourceLatch mouseLatch;
    private readonly ISharedResourceLatch mouseMoveLatch;
    private readonly ConcurrentDictionary<HotkeyGesture, EventArgs> pressedKeys = new();

    private static readonly Binder<UserInputBlocker> Binder = new();

    static UserInputBlocker()
    {
        Binder.Bind(x => x.allInputLatch.IsBusy || x.keyboardLatch.IsBusy).To(x => x.IsKeyboardBlockActive);
        Binder.Bind(x => x.allInputLatch.IsBusy || x.mouseMoveLatch.IsBusy || x.mouseLatch.IsBusy).To(x => x.IsMouseBlockActive);
    }

    public UserInputBlocker(
        IKeyboardEventsSource keyboardEventsSource,
        ISharedResourceLatch allInputLatch,
        ISharedResourceLatch mouseMoveLatch,
        ISharedResourceLatch mouseLatch,
        ISharedResourceLatch keyboardLatch)
    {
        Log.Debug("Initializing user input controller");
        this.mouseMoveLatch = mouseMoveLatch.AddTo(Anchors);
        this.allInputLatch = allInputLatch.AddTo(Anchors);
        this.mouseLatch = mouseLatch.AddTo(Anchors);
        this.keyboardLatch = keyboardLatch.AddTo(Anchors);

        this.WhenAnyValue(x => x.IsKeyboardBlockActive)
            .Select(x => x ? keyboardEventsSource.WhenKeyRaw : Observable.Empty<KeyEventArgsExt>())
            .Switch()
            .SubscribeSafe(HandleKeyboard, Log.HandleUiException)
            .AddTo(Anchors);
            
        this.WhenAnyValue(x => x.IsMouseBlockActive)
            .Select(x => x ? keyboardEventsSource.WhenMouseRaw : Observable.Empty<MouseEventExtArgs>())
            .Switch()
            .SubscribeSafe(HandleMouse, Log.HandleUiException)
            .AddTo(Anchors);
            
        Binder.Attach(this).AddTo(Anchors);
    }
        
    public bool IsKeyboardBlockActive { get; private set; }
        
    public bool IsMouseBlockActive { get; private set; }

    public IDisposable Block(UserInputBlockType userInputBlockType)
    {
        if (userInputBlockType == UserInputBlockType.None)
        {
            return Disposable.Empty;
        }
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