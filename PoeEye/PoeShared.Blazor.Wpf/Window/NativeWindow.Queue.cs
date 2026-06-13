using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PInvoke;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.Wpf;

partial class NativeWindow
{
    /// <summary>
    /// Does not activate the window, and does not discard the mouse message.
    /// </summary>
    private const int MA_NOACTIVATE = 3;

    /// <summary>
    /// In a window currently covered by another window in the same thread (the message will be sent to underlying windows in the same thread until one of them returns a code that is not HTTRANSPARENT).
    /// </summary>
    private static readonly IntPtr HTTRANSPARENT = new(-1);

    private protected void HandleEvent(IWindowEvent windowEvent)
    {
        if (windowEvent is DisposeWindowCommand)
        {
            if (!windowSupplier.IsValueCreated)
            {
                Log.Debug($"Window is not created - ignoring disposal request");
                return;
            }

            var window = GetWindowOrThrow(); //technically we never create the window here, only getting it
            if (window.Anchors.IsDisposed)
            {
                Log.Debug($"Window already disposed - ignoring disposal request");
                return;
            }

            if (isNativeWindowClosingOrClosed)
            {
                Log.Debug($"Native window is already closing or closed - disposing without calling Close: {new {window}}");
                window.DisposeJsSafe();
                return;
            }

            Log.Debug($"Disposing the window: {new {window}}");
            window.Close();
            window.DisposeJsSafe();
        }
        else if (windowEvent is WaitForIdleCommand waitForIdleCommand)
        {
            Log.Debug($"Notifying that queue is processed to this point: {waitForIdleCommand}");
            waitForIdleCommand.ResetEvent.Set();
        }
        else if (windowEvent is InvokeCommand invokeCommand)
        {
            invokeCommand.ActionToExecute();
            invokeCommand.ResetEvent.Set();
        }
        else if (windowEvent is IWindowCommand)
        {
            if (Anchors.IsDisposed)
            {
                Log.Debug($"Ignoring command - already disposed, command: {windowEvent}");
                return;
            }

            if (isClosedTcs.Task.IsCompleted)
            {
                Log.Debug($"Ignoring command - window is closing or already closed, command: {windowEvent}");
                if (windowEvent is ShowDialogCommand showDialogCommand)
                {
                    showDialogCommand.CompletionSource.TrySetResult(true);
                }

                return;
            }

            var window = GetOrCreate();
            switch (windowEvent)
            {
                case SetVisibleCommand command:
                {
                    Log.Debug($"Updating {nameof(IsVisible)} to {command.IsVisible}: {new {window.WindowState}}");
                    if (command.IsVisible)
                    {
                        var ownerHandle = ResolveConfiguredOwnerHandle(window);
                        if (ownerHandle != IntPtr.Zero)
                        {
                            Log.Debug($"Assigning owner handle before showing window: {ownerHandle.ToHexadecimal()}");
                            var windowInteropHelper = new WindowInteropHelper(window);
                            windowInteropHelper.Owner = ownerHandle;
                        }

                        window.Show();
                    }
                    else
                    {
                        window.Hide();
                    }

                    break;
                }
                case ShowDialogCommand command:
                {
                    Log.Debug("Showing the window as a real modal dialog");
                    try
                    {
                        ShowDialogCore(window, command.CancellationToken);
                        command.CompletionSource.TrySetResult(true);
                    }
                    catch (Exception e)
                    {
                        command.CompletionSource.TrySetException(e);
                        throw;
                    }

                    break;
                }
                case ActivateCommand:
                {
                    Log.Debug($"Activating the window");
                    ActivateWindowWhenReady(window, "activate command");
                    break;
                }
                case MinimizeCommand:
                {
                    Log.Debug($"Minimizing the window, current state: {{window.WindowState}}");
                    window.WindowState = WindowState.Minimized;
                    break;
                }
                case MaximizeCommand:
                {
                    Log.Debug($"Maximizing the window, current state: {{window.WindowState}}");
                    window.WindowState = WindowState.Maximized;
                    break;
                }
                case RestoreCommand:
                {
                    Log.Debug($"Restoring the window, current state: {window.WindowState}");
                    window.WindowState = WindowState.Normal;
                    break;
                }
                case CloseCommand:
                {
                    Log.Debug($"Closing the window: {new {window.WindowState}}");
                    window.Close();
                    break;
                }
                case SetWindowTitleCommand command:
                {
                    Log.Debug($"Updating {nameof(window.Title)} to {command.Title}");
                    window.Title = command.Title ?? string.Empty;
                    break;
                }
                case SetWindowState command:
                {
                    Log.Debug($"Updating {nameof(window.WindowState)} to {command.WindowState}");
                    window.WindowState = command.WindowState;
                    break;
                }
                case SetWindowStartupLocationCommand command:
                {
                    Log.Debug($"Updating {nameof(WindowStartupLocation)} to {command.WindowStartupLocation}");
                    window.WindowStartupLocation = command.WindowStartupLocation;
                    ApplyWindowStartupLocation(window, command.WindowStartupLocation);
                    break;
                }
                case SetWindowPosCommand command:
                {
                    if (ShouldLogThrottled(ref lastSetPosLogMs))
                    {
                        Log.Debug($"Setting window position to {command.Location}");
                    }
                    UnsafeNative.SetWindowPos(window.WindowHandle, command.Location);
                    break;
                }
                case SetWindowRectCommand command:
                {
                    if (ShouldLogThrottled(ref lastSetRectLogMs))
                    {
                        Log.Debug($"Setting window rect to {command.Rect}");
                    }
                    UnsafeNative.SetWindowRect(window.WindowHandle, command.Rect);
                    break;
                }
                case StartDragCommand command:
                {
                    Log.Debug($"Starting dragging the window");
                    dragAnchor.Disposable = command.Anchor;
                    try
                    {
                        new BlazorWindowMouseDragController(this, window.ContentControl).AddTo(command.Anchor);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to start window dragging", e);
                        dragAnchor.Disposable = null;
                        throw;
                    }
                    break;
                }
                case StartResizeCommand command:
                {
                    Log.Debug($"Starting resizing the window from {command.Direction}");
                    dragAnchor.Disposable = command.Anchor;
                    try
                    {
                        new BlazorWindowEdgeResizeController(this, window.ContentControl, command.Direction).AddTo(command.Anchor);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to start window resizing from {command.Direction}", e);
                        dragAnchor.Disposable = null;
                        throw;
                    }
                    break;
                }
                case SetWindowSizeCommand command:
                {
                    if (ShouldLogThrottled(ref lastSetSizeLogMs))
                    {
                        Log.Debug($"Setting window size to {command.Size}");
                    }
                    UnsafeNative.SetWindowSize(window.WindowHandle, command.Size);
                    break;
                }
                case SetShowTitleBarCommand command:
                {
                    Log.Debug($"Updating {nameof(TitleBarDisplayMode)} to {command.TitleBarDisplayMode}");
                    window.UpdateTitleBarDisplayMode(command.TitleBarDisplayMode);
                    break;
                }
                case SetAllowsTransparency command:
                {
                    Log.Debug($"Updating {nameof(AllowsTransparency)} to {command.AllowsTransparency}");
                    window.UpdateAllowsTransparency(command.AllowsTransparency);
                    break;
                }
                case SetWindowPadding command:
                {
                    Log.Debug($"Updating {nameof(Padding)} to {command.Padding}");
                    // Keep a 1px safety margin on most edges to avoid WebView cropping caused by WPF rounding.
                    // For frameless/custom titlebars, forcing a top margin creates a visible strip above the Blazor titlebar.
                    var effectiveTitleBarDisplayMode = command.TitleBarDisplayMode.ResolveForWpf();
                    var minTopPadding = effectiveTitleBarDisplayMode is TitleBarDisplayMode.Custom or TitleBarDisplayMode.None
                        ? 0
                        : 1;
                    window.ContentControl.Margin = new Thickness(
                        left: Math.Max(command.Padding.Left, 1),
                        top: Math.Max(command.Padding.Top, minTopPadding),
                        right: Math.Max(command.Padding.Right, 1),
                        bottom: Math.Max(command.Padding.Bottom, 1)
                    );
                    break;
                }
                case SetResizeMode command:
                {
                    Log.Debug($"Updating {nameof(ResizeMode)} to {command.ResizeMode}");
                    window.ResizeMode = command.ResizeMode;
                    if (TitleBarDisplayMode.ResolveForWpf() == TitleBarDisplayMode.System)
                    {
                        window.UpdateTitleBarDisplayMode(TitleBarDisplayMode);
                    }
                    break;
                }
                case SetShowInTaskbar command:
                {
                    Log.Debug($"Updating {nameof(ShowInTaskbar)} to {command.ShowInTaskbar}");
                    window.ShowInTaskbar = command.ShowInTaskbar;
                    break;
                }
                case SetShowActivated command:
                {
                    Log.Debug($"Updating {nameof(ShowActivated)} to {command.ShowActivated}");
                    window.ShowActivated = command.ShowActivated;
                    break;
                }
                case SetIsClickThrough command:
                {
                    if (command.IsClickThrough && !AllowsTransparency)
                    {
                        Log.Warn($"{nameof(IsClickThrough)} requires {nameof(AllowsTransparency)} to be enabled");
                        break;
                    }

                    var overlayMode = command.IsClickThrough ? OverlayMode.Transparent : OverlayMode.Layered;
                    Log.Debug($"Updating OverlayMode to {overlayMode}");
                    window.SetOverlayMode(overlayMode);
                    break;
                }
                case SetOpacity command:
                {
                    Log.Debug($"Updating {nameof(Opacity)} to {command.Opacity}");
                    var calculatedOpacity = command.Opacity <= 0
                        ? 0.01d //true transparent window is non-clickable, got 0.01 is from testing as min value
                        : command.Opacity;
                    window.Opacity = calculatedOpacity;
                    break;
                }
                case SetBackgroundColor command:
                {
                    Log.Debug($"Updating {nameof(BackgroundColor)} to {command.BackgroundColor}");
                    var calculatedColor = command.BackgroundColor.A == 0
                        ? command.BackgroundColor with {A = 1} //fully transparent window is non-clickable
                        : command.BackgroundColor;
                    var color = new SolidColorBrush(calculatedColor);
                    color.Freeze();
                    window.Background = color;
                    break;
                }
                case SetBorderColor command:
                {
                    Log.Debug($"Updating {nameof(BorderColor)} to {command.BorderColor}");
                    var color = new SolidColorBrush(command.BorderColor);
                    color.Freeze();
                    window.BorderBrush = color;
                    break;
                }
                case SetBorderThickness command:
                {
                    Log.Debug($"Updating {nameof(BorderThickness)} to {command.BorderThickness}");
                    var effectiveTitleBarDisplayMode = command.TitleBarDisplayMode.ResolveForWpf();
                    if (effectiveTitleBarDisplayMode is TitleBarDisplayMode.Custom or TitleBarDisplayMode.None)
                    {
                        window.BorderThickness = new Thickness(0);
                    }
                    else if (effectiveTitleBarDisplayMode == TitleBarDisplayMode.System)
                    {
                        window.BorderThickness = command.BorderThickness;
                        window.UpdateTitleBarDisplayMode(command.TitleBarDisplayMode);
                    }
                    else
                    {
                        window.BorderThickness = command.BorderThickness;
                    }

                    break;
                }
                case SetTopmostCommand command:
                {
                    Log.Debug($"Updating {nameof(Topmost)} to {command.Topmost}");
                    window.Topmost = command.Topmost;
                    break;
                }
                case SetNoActivate command:
                {
                    Log.Debug($"Updating {nameof(NoActivate)} to {command.NoActivate}");
                    window.SetActivation(command.NoActivate == false);
                    break;
                }
                case SetContentCommand command:
                {
                    Log.Debug($"Updating hosted content using factory: {command.ContentFactory}");
                    window.UpdateHostedContent(command.ContentFactory);
                    break;
                }
                default:
                {
                    if (!HandleCommand(windowEvent, window))
                    {
                        throw new ArgumentOutOfRangeException(nameof(windowEvent), $@"Unsupported event type: {windowEvent.GetType()}");
                    }

                    break;
                }
            }
        }
        else
        {
            if (Anchors.IsDisposed)
            {
                Log.Debug($"Ignoring event notification - already disposed: {windowEvent}");
                return;
            }

            switch (windowEvent)
            {
                case IsVisibleChangedEvent args:
                {
                    windowVisible.SetValue(args.IsVisible, TrackedPropertyUpdateSource.Internal);
                    break;
                }
                case WindowSizeChangedEvent args:
                {
                    windowWidth.SetValue(args.Size.Width, TrackedPropertyUpdateSource.Internal);
                    windowHeight.SetValue(args.Size.Height, TrackedPropertyUpdateSource.Internal);
                    break;
                }
                case WindowPosChangedEvent args:
                {
                    windowLeft.SetValue(args.Location.X, TrackedPropertyUpdateSource.Internal);
                    windowTop.SetValue(args.Location.Y, TrackedPropertyUpdateSource.Internal);
                    break;
                }
                case WindowTitleChangedEvent titleChangedEvent:
                {
                    windowTitle.SetValue(titleChangedEvent.Title, TrackedPropertyUpdateSource.Internal);
                    break;
                }
                case WindowStateChangedEvent args:
                {
                    windowState.SetValue(args.WindowState, TrackedPropertyUpdateSource.Internal);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(windowEvent), $@"Unsupported event type: {windowEvent.GetType()}");
            }
        }
    }

    /// <summary>
    /// Extension point for subclasses (e.g. BlazorWindow) to handle their own window commands.
    /// Invoked on the window's UI thread as a part of the command queue processing.
    /// </summary>
    /// <returns>true when the command was handled, false otherwise</returns>
    private protected virtual bool HandleCommand(IWindowEvent windowEvent, WindowView window)
    {
        return false;
    }

    /// <summary>
    /// Propagates the initial state of all tracked properties into the freshly created window view.
    /// Subclasses may extend this with their own initial commands - they must call the base implementation first.
    /// </summary>
    private protected virtual void HandleInitialState(WindowView window)
    {
        HandleEvent(new SetShowInTaskbar(ShowInTaskbar));
        HandleEvent(new SetShowActivated(ShowActivated));
        HandleEvent(new SetTopmostCommand(Topmost));
        HandleEvent(new SetAllowsTransparency(AllowsTransparency));
        HandleEvent(new SetBackgroundColor(BackgroundColor));
        HandleEvent(new SetBorderThickness(TitleBarDisplayMode, BorderThickness));
        HandleEvent(new SetBorderColor(BorderColor));
        HandleEvent(new SetWindowPadding(TitleBarDisplayMode, Padding));
        HandleEvent(new SetResizeMode(ResizeMode));
        HandleEvent(new SetWindowTitleCommand(Title));
        HandleEvent(new SetWindowState(WindowState));
        HandleEvent(new SetShowTitleBarCommand(
            TitleBarDisplayMode,
            ShowCloseButton: ShowCloseButton,
            ShowMinButton: ShowMinButton,
            ShowMaxButton: ShowMaxButton));

        var contentFactory = ContentFactory;
        if (contentFactory != null)
        {
            HandleEvent(new SetContentCommand(contentFactory));
        }
    }

    /// <summary>
    /// Subscribes to content-specific property changes and input events.
    /// The default implementation propagates <see cref="ContentFactory"/> updates and tracks
    /// keyboard/mouse events on the window itself. BlazorWindow replaces this with WebView-based tracking.
    /// </summary>
    private protected virtual void SubscribeToContentEvents(WindowView window, IObserver<IWindowEvent> observer, CompositeDisposable anchors)
    {
        this.WhenAnyValue(x => x.ContentFactory)
            .Skip(1)
            .Subscribe(x => observer.OnNext(new SetContentCommand(x)))
            .AddTo(anchors);

        // events propagation - for native windows input events are tracked on the window itself
        SubscribeToInputEvents(window, anchors);
    }

    private IObservable<IWindowEvent> SubscribeToWindow(IFluentLog log, WindowView window)
    {
        return Observable.Create<IWindowEvent>(observer =>
        {
            log.Debug($"Creating new window and subscriptions");

            var anchors = new CompositeDisposable();
            Disposable.Create(() => log.Debug("Window subscription is being disposed")).AddTo(anchors);

            TryPrepareTitleBarDisplayMode(TitleBarDisplayMode);

            //SetupInitialState in Window is called BEFORE that SourceInitialized
            //so to set up proper initial size and position (for the very first frame)
            //we have to "guess" current DPI, then, when SourceInitialized will be called, it will be re-calculated again
            //best-case scenario - DPI won't change and there will be no blinking at all
            HandleInitialState(window);
            UpdateWindowBoundsFromMonitor(IntPtr.Zero);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.SourceInitialized += h, h => window.SourceInitialized -= h)
                .Subscribe(() =>
                {
                    try
                    {
                        var windowHandle = window.WindowHandle;
                        if (windowHandle == IntPtr.Zero)
                        {
                            throw new InvalidOperationException("HwndSource must be initialized at this point");
                        }

                        UpdateWindowBoundsFromMonitor(windowHandle);
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Failed to reposition window on SourceInitialized", e);
                        throw;
                    }
                })
                .AddTo(anchors);

            window.WhenLoaded()
                .Subscribe(() =>
                {
                    try
                    {
                        var source = (HwndSource) PresentationSource.FromVisual(window);
                        if (source == null)
                        {
                            throw new InvalidOperationException("HwndSource must be initialized at this point");
                        }

                        source.AddHook(WindowHook);
                        Disposable.Create(() =>
                        {
                            try
                            {
                                source.RemoveHook(WindowHook);
                            }
                            catch (Exception e)
                            {
                                Log.Warn("Failed to remove window hook", e);
                            }
                        }).AddTo(anchors);
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Failed to add window hook", e);
                        throw;
                    }
                })
                .AddTo(anchors);

            //these events are internally using WPF window subsystem and probably should be moved to WindowHook
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.StateChanged += h, h => window.StateChanged -= h)
                .Select(x => window.WindowState)
                .Subscribe(x => observer.OnNext(new WindowStateChangedEvent(x)))
                .AddTo(anchors);

            // Track visibility from the managed WPF window lifecycle so the state stays correct
            // even if the native hook misses the initial show/hide transition.
            Observable
                .FromEventPattern<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    h => window.IsVisibleChanged += h,
                    h => window.IsVisibleChanged -= h)
                .Select(_ => window.IsVisible)
                .DistinctUntilChanged()
                .Subscribe(x => observer.OnNext(new IsVisibleChangedEvent(x)))
                .AddTo(anchors);

            //size/location-related events are handled in a special way - to avoid blinking, they are set BEFORE form is loaded
            windowLeft
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowPosCommand(new Point()
                {
                    X = p.Value,
                    Y = Top,
                })))
                .AddTo(anchors);

            windowTop
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowPosCommand(new Point()
                {
                    X = Left,
                    Y = p.Value,
                })))
                .AddTo(anchors);

            windowWidth
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowSizeCommand(new Size()
                {
                    Width = p.Value,
                    Height = Height
                })))
                .AddTo(anchors);

            windowHeight
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowSizeCommand(new Size()
                {
                    Width = Width,
                    Height = p.Value
                })))
                .AddTo(anchors);

            windowTopmost
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetTopmostCommand(x.Value)))
                .AddTo(anchors);

            windowTitle
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetWindowTitleCommand(x.Value)))
                .AddTo(anchors);

            showInTaskbar
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetShowInTaskbar(x.Value)))
                .AddTo(anchors);

            showActivated
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetShowActivated(x.Value)))
                .AddTo(anchors);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.TitleBarDisplayMode),
                    this.WhenAnyValue(x => x.Padding),
                    (titleBarDisplayMode, borderThickness) =>
                        new SetWindowPadding(titleBarDisplayMode, borderThickness))
                .Skip(1)
                .Subscribe(x => observer.OnNext(x))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.BackgroundColor)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBackgroundColor(x)))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.BorderColor)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBorderColor(x)))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.AllowsTransparency)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetAllowsTransparency(x)))
                .AddTo(anchors);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.TitleBarDisplayMode),
                    this.WhenAnyValue(x => x.BorderThickness),
                    (titleBarDisplayMode, borderThickness) =>
                        new SetBorderThickness(titleBarDisplayMode, borderThickness))
                .Skip(1)
                .Subscribe(x => observer.OnNext(x))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.ResizeMode)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetResizeMode(x)))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.WindowState)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetWindowState(x)))
                .AddTo(anchors);

            this.WhenAnyValue(x => x.WindowStartupLocation)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetWindowStartupLocationCommand(x)))
                .AddTo(anchors);

            window.WhenLoaded()
                .Subscribe(() =>
                {
                    //some properties could be set only AFTER window is loaded
                    this.WhenAnyValue(x => x.NoActivate)
                        .Subscribe(x => observer.OnNext(new SetNoActivate(x)))
                        .AddTo(anchors);

                    //to avoid System.InvalidOperationException: Transparent mode requires AllowsTransparency to be set to True
                    this.WhenAnyValue(x => x.IsClickThrough)
                        .Subscribe(x => observer.OnNext(new SetIsClickThrough(x)))
                        .AddTo(anchors);

                    this.WhenAnyValue(x => x.Opacity)
                        .Subscribe(x => observer.OnNext(new SetOpacity(x)))
                        .AddTo(anchors);

                    if (ShowActivated)
                    {
                        Activate();
                    }
                })
                .AddTo(anchors);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.TitleBarDisplayMode),
                    this.WhenAnyValue(x => x.ShowCloseButton),
                    this.WhenAnyValue(x => x.ShowMinButton),
                    this.WhenAnyValue(x => x.ShowMaxButton),
                    (titleBarDisplayMode, showCloseButton, showMinButton, showMaxButton) =>
                        new SetShowTitleBarCommand(titleBarDisplayMode, ShowCloseButton: showCloseButton, ShowMinButton: showMinButton, ShowMaxButton: showMaxButton)
                )
                .Skip(1)
                .Subscribe(x => { observer.OnNext(x); })
                .AddTo(anchors);

            windowVisible
                .Listen()
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetVisibleCommand(x.Value)))
                .AddTo(anchors);

            // content-specific property subscriptions + input events propagation
            SubscribeToContentEvents(window, observer, anchors);

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => Loaded?.Invoke(this, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Unloaded += h, h => window.Unloaded -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => Unloaded?.Invoke(this, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    Log.Debug($"Native Window has been closed");
                    isNativeWindowClosingOrClosed = true;
                    MarkWindowAsClosingOrClosed("native window closed");

                    if (!window.IsDisposed)
                    {
                        Log.Debug($"Disposing native window");
                        window.DisposeJsSafe();
                    }

                    Closed?.Invoke(this, x);
                })
                .AddTo(anchors);

            Observable
                .FromEventPattern<CancelEventHandler, CancelEventArgs>(h => window.Closing += h, h => window.Closing -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    var wasNativeWindowClosingOrClosed = isNativeWindowClosingOrClosed;
                    isNativeWindowClosingOrClosed = true;
                    Closing?.Invoke(this, x);
                    if (x.Cancel)
                    {
                        isNativeWindowClosingOrClosed = wasNativeWindowClosingOrClosed;
                        return;
                    }

                    MarkWindowAsClosingOrClosed("native window closing");
                })
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Activated += h, h => window.Activated -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => Activated?.Invoke(this, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Deactivated += h, h => window.Deactivated -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => Deactivated?.Invoke(this, x))
                .AddTo(anchors);

            if (!Anchors.IsDisposed)
            {
                var interop = new WindowInteropHelper(window);
                var windowHandle = interop.EnsureHandle();
                if (windowHandle == IntPtr.Zero)
                {
                    throw new InvalidStateException("Failed to get created window handle");
                }

                if (window.WindowHandle == IntPtr.Zero)
                {
                    throw new InvalidStateException("Created window handle is zero");
                }

                log.AddSuffix($"Wnd {windowHandle.ToHexadecimal()}");
                log.Debug($"Created new window");
            }
            else
            {
                log.Warn($"Failed to create window - already disposed");
            }

            Disposable.Create(() => log.Debug("Window subscription has been disposed")).AddTo(anchors);
            return anchors;
        });

        void UpdateWindowBoundsFromMonitor(IntPtr hwnd)
        {
            try
            {
                if (!UnsafeNative.IsWindows10OrGreater())
                {
                    // SHCore is supported only on Win8.1+, it's safer to fallback to Win10
                    log.Warn($"Failed to set initial window position - OS is not supported");
                    return;
                }

                var desktopMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
                if (desktopMonitor == IntPtr.Zero)
                {
                    log.Warn($"Failed to set initial window position - could not find desktop monitor");
                    return;
                }

                var dpiResult =
                    SHCore.GetDpiForMonitor(desktopMonitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY);
                if (dpiResult.Failed)
                {
                    log.Warn($"Failed to GetDpiForMonitor and set initial window position, hrResult: {dpiResult}");
                    return;
                }

                SetWindowStartupLocation(hwnd, WindowStartupLocation);
                AssignWindowBounds(scaleX: 96d / dpiX, scaleY: 96d / dpiY);
            }
            catch (Exception e)
            {
                log.Warn("Failed to set initial window position", e);
            }
        }

        void AssignWindowBounds(double scaleX, double scaleY)
        {
            var desiredWidth = Width * scaleX;
            if (!double.IsFinite(window.Width) || Math.Abs(desiredWidth - window.Width) > 0.5)
            {
                window.Width = desiredWidth;
            }

            var desiredHeight = Height * scaleY;
            if (!double.IsFinite(window.Height) || Math.Abs(desiredHeight - window.Height) > 0.5)
            {
                window.Height = desiredHeight;
            }

            var desiredLeft = Left * scaleX;
            if (!double.IsFinite(window.Left) || Math.Abs(desiredLeft - window.Left) > 0.5)
            {
                window.Left = desiredLeft;
            }

            var desiredTop = Top * scaleY;
            if (!double.IsFinite(window.Top) || Math.Abs(desiredTop - window.Top) > 0.5)
            {
                window.Top = desiredTop;
            }
        }

        void CenterWindowWithin(Rectangle monitorBounds)
        {
            var left = monitorBounds.X + (monitorBounds.Width - Width) / 2;
            if (left != Left)
            {
                Left = left;
            }

            var top = monitorBounds.Y + (monitorBounds.Height - Height) / 2;
            if (top != Top)
            {
                Top = top;
            }
        }

        void SetWindowStartupLocation(IntPtr hwnd, WindowStartupLocation startupLocation)
        {
            switch (startupLocation)
            {
                case WindowStartupLocation.CenterScreen:
                {
                    var desktopMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
                    if (desktopMonitor == IntPtr.Zero)
                    {
                        Log.Warn($"Failed to set initial window size - could not find desktop monitor");
                        break;
                    }

                    if (!User32.GetMonitorInfo(desktopMonitor, out var monitorInfo))
                    {
                        log.Warn($"Failed to set initial window size - could not get rect of desktop monitor {desktopMonitor.ToHexadecimal()}");
                        break;
                    }

                    var monitorRect = monitorInfo.rcMonitor;
                    var monitorBounds = Rectangle.FromLTRB(monitorRect.left, monitorRect.top, monitorRect.right, monitorRect.bottom);
                    log.Debug($"Centering window within monitor {monitorBounds}");
                    CenterWindowWithin(monitorBounds);
                    break;
                }
                case WindowStartupLocation.CenterOwner:
                {
                    if (!TryGetOwnerBounds(out var windowBounds))
                    {
                        log.Warn("Owner handle is not set, centering within screen");
                        SetWindowStartupLocation(hwnd, WindowStartupLocation.CenterScreen);
                        return;
                    }
                    log.Debug($"Centering window within window bounds {windowBounds}");
                    CenterWindowWithin(windowBounds);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Hooks keyboard/mouse events of the given element and forwards them into the window-level events.
    /// For native windows the window itself is used as the source, for Blazor windows - the WebViews.
    /// </summary>
    private protected void SubscribeToInputEvents(UIElement inputEventSource, CompositeDisposable anchors)
    {
        Observable
            .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => inputEventSource.MouseDown += h, h => inputEventSource.MouseDown -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => MouseDown?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => inputEventSource.MouseUp += h, h => inputEventSource.MouseUp -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => MouseUp?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => inputEventSource.PreviewMouseDown += h, h => inputEventSource.PreviewMouseDown -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => PreviewMouseDown?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => inputEventSource.PreviewMouseUp += h, h => inputEventSource.PreviewMouseUp -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => PreviewMouseUp?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<MouseEventHandler, MouseEventArgs>(h => inputEventSource.MouseMove += h, h => inputEventSource.MouseMove -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => MouseMove?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<MouseEventHandler, MouseEventArgs>(h => inputEventSource.PreviewMouseMove += h, h => inputEventSource.PreviewMouseMove -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => PreviewMouseMove?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => inputEventSource.KeyDown += h, h => inputEventSource.KeyDown -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => KeyDown?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => inputEventSource.KeyUp += h, h => inputEventSource.KeyUp -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => KeyUp?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => inputEventSource.PreviewKeyDown += h, h => inputEventSource.PreviewKeyDown -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => PreviewKeyDown?.Invoke(this, x))
            .AddTo(anchors);

        Observable
            .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => inputEventSource.PreviewKeyUp += h, h => inputEventSource.PreviewKeyUp -= h)
            .Select(x => x.EventArgs)
            .Subscribe(x => PreviewKeyUp?.Invoke(this, x))
            .AddTo(anchors);
    }


    private IntPtr WindowHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var msg = (User32.WindowMessage) msgRaw;
        // Several native lifecycle messages use lParam == 0, so keep them visible to the tracking layer.
        if (handled || (lParam == IntPtr.Zero && msg is not User32.WindowMessage.WM_SHOWWINDOW and not User32.WindowMessage.WM_EXITSIZEMOVE))
        {
            return IntPtr.Zero;
        }

        try
        {
            switch (msg)
            {
                case User32.WindowMessage.WM_SHOWWINDOW:
                    var isVisible = wParam != IntPtr.Zero;
                    EnqueueUpdate(new IsVisibleChangedEvent(isVisible));
                    break;
                case User32.WindowMessage.WM_EXITSIZEMOVE:
                {
                    dragAnchor.Disposable = null;
                    break;
                }
                case User32.WindowMessage.WM_GETICON:
                {
                    handled = true;
                    break;
                }
                case User32.WindowMessage.WM_NCHITTEST:
                {
                    if (IsClickThrough)
                    {
                        //this makes the window transparent to GetWindowFromPoint (as usually expected for non-interactive windows)
                        handled = true;
                        return HTTRANSPARENT;
                    }

                    break;
                }
                case User32.WindowMessage.WM_MOUSEACTIVATE:
                {
                    if (NoActivate)
                    {
                        handled = true;
                        return new IntPtr(MA_NOACTIVATE);
                    }

                    break;
                }
                case User32.WindowMessage.WM_GETMINMAXINFO
                    when Marshal.PtrToStructure(lParam, typeof(User32.MINMAXINFO)) is User32.MINMAXINFO minmax:
                {
                    if (MinWidth > 0)
                    {
                        minmax.ptMinTrackSize.x = MinWidth;
                    }

                    if (MinHeight > 0)
                    {
                        minmax.ptMinTrackSize.y = MinHeight;
                    }

                    if (MaxWidth > 0)
                    {
                        minmax.ptMaxTrackSize.x = MaxWidth;
                    }

                    if (MaxHeight > 0)
                    {
                        minmax.ptMaxTrackSize.y = MaxHeight;
                    }

                    Marshal.StructureToPtr(minmax, lParam, true);
                    handled = true;
                    break;
                }
                case User32.WindowMessage.WM_SETTEXT:
                {
                    // lParam points to the new window text
                    var newTitle = Marshal.PtrToStringUni(lParam);
                    EnqueueUpdate(new WindowTitleChangedEvent(newTitle));
                    break;
                }
                case User32.WindowMessage.WM_WINDOWPOSCHANGING
                    when Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS)) is UnsafeNative.WINDOWPOS wp:
                {
                    if (wp.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE))
                    {
                        break;
                    }

                    var updatesCount = 0;
                    if (!wp.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOMOVE))
                    {
                        eventQueue.Add(new WindowPosChangedEvent(new Point(wp.x, wp.y)));
                        updatesCount++;
                    }

                    if (!wp.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOSIZE))
                    {
                        eventQueue.Add(new WindowSizeChangedEvent(new Size(wp.cx, wp.cy)));
                        updatesCount++;
                    }

                    if (updatesCount > 0)
                    {
                        EnqueueUpdate();
                    }

                    break;
                }

                case User32.WindowMessage.WM_SIZE:
                {
                    var width = lParam.LoWord();
                    var height = lParam.HiWord();
                    EnqueueUpdate(new WindowSizeChangedEvent(new Size(width, height)));
                    break;
                }

                case User32.WindowMessage.WM_MOVE:
                {
                    // WM_MOVE carries signed screen coordinates, so negative monitor positions must stay negative.
                    var x = lParam.SignedLoWord();
                    var y = lParam.SignedHiWord();
                    EnqueueUpdate(new WindowPosChangedEvent(new Point(x, y)));
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("Exception in wnd procedure", e);
            throw;
        }

        return IntPtr.Zero;
    }

    private void ApplyWindowStartupLocation(WindowView window, WindowStartupLocation startupLocation)
    {
        if (window.WindowHandle == IntPtr.Zero || startupLocation == WindowStartupLocation.Manual)
        {
            return;
        }

        var windowBounds = UnsafeNative.GetWindowRect(window.WindowHandle);
        if (windowBounds.Width <= 0 || windowBounds.Height <= 0)
        {
            return;
        }

        Rectangle targetBounds;
        switch (startupLocation)
        {
            case WindowStartupLocation.CenterScreen:
                if (!TryGetMonitorBounds(window.WindowHandle, out targetBounds))
                {
                    return;
                }
                break;
            case WindowStartupLocation.CenterOwner:
                if (!TryGetOwnerBounds(out targetBounds) && !TryGetMonitorBounds(window.WindowHandle, out targetBounds))
                {
                    return;
                }
                break;
            default:
                return;
        }

        var targetLocation = new Point(
            targetBounds.X + (targetBounds.Width - windowBounds.Width) / 2,
            targetBounds.Y + (targetBounds.Height - windowBounds.Height) / 2);
        UnsafeNative.SetWindowPos(window.WindowHandle, targetLocation);
    }

    private bool TryGetMonitorBounds(IntPtr hwnd, out Rectangle monitorBounds)
    {
        var desktopMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
        if (desktopMonitor == IntPtr.Zero)
        {
            Log.Warn("Failed to find desktop monitor for startup location update");
            monitorBounds = default;
            return false;
        }

        if (!User32.GetMonitorInfo(desktopMonitor, out var monitorInfo))
        {
            Log.Warn($"Failed to get monitor info for startup location update: {desktopMonitor.ToHexadecimal()}");
            monitorBounds = default;
            return false;
        }

        var monitorRect = monitorInfo.rcMonitor;
        monitorBounds = Rectangle.FromLTRB(monitorRect.left, monitorRect.top, monitorRect.right, monitorRect.bottom);
        return true;
    }

    private bool TryGetOwnerBounds(out Rectangle ownerBounds)
    {
        IntPtr ownerHandle;
        if (dialogOwnerHandle != IntPtr.Zero)
        {
            ownerHandle = dialogOwnerHandle;
        }
        else
        {
            ownerHandle = OwnerHandle;
            if (ownerHandle == IntPtr.Zero)
            {
                try
                {
                    ownerHandle = UnsafeNative.ResolveParentForDialogWindow();
                    if (ownerHandle != IntPtr.Zero)
                    {
                        Log.Debug($"CenterOwner fallback resolved owner handle for startup location update: {ownerHandle.ToHexadecimal()}");
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to resolve fallback owner handle for startup location update", e);
                    ownerBounds = default;
                    return false;
                }
            }
        }

        if (ownerHandle == IntPtr.Zero)
        {
            ownerBounds = default;
            return false;
        }

        if (!User32.IsWindow(ownerHandle))
        {
            Log.Warn($"Configured owner handle is invalid for startup location update: {ownerHandle.ToHexadecimal()}");
            ownerBounds = default;
            return false;
        }

        if (!User32.GetWindowRect(ownerHandle, out var ownerRect))
        {
            Log.Warn($"Failed to get owner rect for startup location update: {ownerHandle.ToHexadecimal()}");
            ownerBounds = default;
            return false;
        }

        ownerBounds = Rectangle.FromLTRB(ownerRect.left, ownerRect.top, ownerRect.right, ownerRect.bottom);
        return true;
    }

    private IntPtr ResolveConfiguredOwnerHandle(WindowView window)
    {
        var ownerHandle = OwnerHandle;
        if (ownerHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        if (!User32.IsWindow(ownerHandle))
        {
            Log.Warn($"Configured owner handle is invalid, ignoring owner: {ownerHandle.ToHexadecimal()}");
            return IntPtr.Zero;
        }

        if (window.WindowHandle != IntPtr.Zero && ownerHandle == window.WindowHandle)
        {
            Log.Warn("Configured owner handle points to the target window itself, ignoring owner");
            return IntPtr.Zero;
        }

        return ownerHandle;
    }

    private sealed record WaitForIdleCommand(ManualResetEventSlim ResetEvent, DateTimeOffset Timestamp) : IWindowCommand;

    private sealed record ShowDialogCommand(CancellationToken CancellationToken, TaskCompletionSource<bool> CompletionSource) : IWindowCommand;

    private sealed record InvokeCommand(Action ActionToExecute, ManualResetEventSlim ResetEvent, DateTimeOffset Timestamp) : IWindowCommand;

    private sealed record ActivateCommand : IWindowCommand;

    private sealed record CloseCommand : IWindowCommand;

    private sealed record MinimizeCommand : IWindowCommand;

    private sealed record MaximizeCommand : IWindowCommand;

    private sealed record RestoreCommand : IWindowCommand;

    private sealed record DisposeWindowCommand : IWindowCommand;

    private sealed record SetWindowState(WindowState WindowState) : IWindowCommand;

    private sealed record SetWindowStartupLocationCommand(WindowStartupLocation WindowStartupLocation) : IWindowCommand;

    private sealed record SetWindowTitleCommand(string Title) : IWindowCommand;

    private sealed record SetWindowRectCommand(Rectangle Rect) : IWindowCommand;

    private sealed record StartDragCommand(CompositeDisposable Anchor) : IWindowCommand;

    private sealed record StartResizeCommand(WindowResizeDirection Direction, CompositeDisposable Anchor) : IWindowCommand;

    private sealed record SetWindowPosCommand(Point Location) : IWindowCommand;

    private sealed record SetWindowSizeCommand(Size Size) : IWindowCommand;

    private sealed record SetTopmostCommand(bool Topmost) : IWindowCommand;

    private sealed record SetNoActivate(bool NoActivate) : IWindowCommand;

    private sealed record SetVisibleCommand(bool IsVisible) : IWindowCommand;

    private sealed record SetWindowPadding(TitleBarDisplayMode TitleBarDisplayMode, Thickness Padding) : IWindowCommand;

    private sealed record SetBorderThickness(TitleBarDisplayMode TitleBarDisplayMode, Thickness BorderThickness) : IWindowCommand;

    private sealed record SetAllowsTransparency(bool AllowsTransparency) : IWindowCommand;

    private sealed record SetResizeMode(ResizeMode ResizeMode) : IWindowCommand;

    private sealed record SetShowTitleBarCommand(TitleBarDisplayMode TitleBarDisplayMode, bool ShowCloseButton, bool ShowMinButton, bool ShowMaxButton) : IWindowCommand;

    private sealed record SetShowInTaskbar(bool ShowInTaskbar) : IWindowCommand;

    private sealed record SetShowActivated(bool ShowActivated) : IWindowCommand;

    private sealed record SetIsClickThrough(bool IsClickThrough) : IWindowCommand;

    private sealed record SetOpacity(double Opacity) : IWindowCommand;

    private sealed record SetBackgroundColor(Color BackgroundColor) : IWindowCommand;

    private sealed record SetBorderColor(Color BorderColor) : IWindowCommand;

    private sealed record SetContentCommand(Func<INativeWindow, UIElement> ContentFactory) : IWindowCommand;

    private sealed record IsVisibleChangedEvent(bool IsVisible) : IWindowEvent;

    private sealed record WindowTitleChangedEvent(string Title) : IWindowEvent;

    private sealed record WindowPosChangedEvent(Point Location) : IWindowEvent;

    private sealed record WindowSizeChangedEvent(Size Size) : IWindowEvent;

    private sealed record WindowStateChangedEvent(WindowState WindowState) : IWindowEvent;

    private protected interface IWindowCommand : IWindowEvent
    {
    }

    private protected interface IWindowEvent
    {
    }
}
