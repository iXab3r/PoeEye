using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Wpf;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;
using Color = System.Windows.Media.Color;
using KeyEventArgsForms = System.Windows.Forms.KeyEventArgs;
using KeyEventHandlerForms = System.Windows.Forms.KeyEventHandler;
using KeyPressEventArgsForms = System.Windows.Forms.KeyPressEventArgs;
using KeyPressEventHandlerForms = System.Windows.Forms.KeyPressEventHandler;
using MouseEventArgsForms = System.Windows.Forms.MouseEventArgs;
using MouseEventHandlerForms = System.Windows.Forms.MouseEventHandler;
using MouseButtonsForms = System.Windows.Forms.MouseButtons;
using Point = System.Drawing.Point;
using PreviewKeyDownEventArgsForms = System.Windows.Forms.PreviewKeyDownEventArgs;
using PreviewKeyDownEventHandlerForms = System.Windows.Forms.PreviewKeyDownEventHandler;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.WinForms;

partial class BlazorWindow
{
    /// <summary>
    /// Does not activate the window, and does not discard the mouse message.
    /// </summary>
    private const int MA_NOACTIVATE = 3;

    /// <summary>
    /// In a window currently covered by another window in the same thread (the message will be sent to underlying windows in the same thread until one of them returns a code that is not HTTRANSPARENT).
    /// </summary>
    private static readonly IntPtr HTTRANSPARENT = new(-1);

    private void HandleEvent(IWindowEvent windowEvent)
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

            Log.Debug($"Disposing the window: {new {window}}");
            window.Close();
            window.Dispose();
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

            var window = GetOrCreate();
            switch (windowEvent)
            {
                case SetVisibleCommand command:
                {
                    Log.Debug($"Updating {nameof(IsVisible)} to {command.IsVisible}: {new {window.WindowState}}");
                    if (command.IsVisible)
                    {
                        window.Show();
                    }
                    else
                    {
                        window.Hide();
                    }

                    break;
                }
                case ActivateCommand:
                {
                    Log.Debug($"Activating the window");
                    ActivateWindowWhenReady(window, "activate command");
                    break;
                }
                case ShowDevToolsCommand:
                {
                    Log.Debug($"Showing dev tools");
                    window.ContentControl.OpenDevTools().AndForget();
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
                    dragAnchor.Disposable = null;
                    dragAnchor.Disposable = new BlazorWindowMouseDragController(this, window.ContentControl).AddTo(command.Anchor);
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
                    window.ApplyWindowPadding(command.Padding);
                    break;
                }
                case SetResizeMode command:
                {
                    Log.Debug($"Updating {nameof(ResizeMode)} to {command.ResizeMode}");
                    window.ApplyResizeMode(command.ResizeMode);
                    if (TitleBarDisplayMode == TitleBarDisplayMode.System)
                    {
                        window.UpdateTitleBarDisplayMode(TitleBarDisplayMode.System);
                    }

                    break;
                }
                case SetShowInTaskbar command:
                {
                    Log.Debug($"Updating {nameof(ShowInTaskbar)} to {command.ShowInTaskbar}");
                    window.ShowInTaskbar = command.ShowInTaskbar;
                    break;
                }
                case SetIsClickThrough command:
                {
                    if (command.IsClickThrough && !AllowsTransparency)
                    {
                        Log.Warn($"{nameof(IsClickThrough)} requires {nameof(AllowsTransparency)} to be enabled");
                        break;
                    }

                    // WebView2 hosted in WinForms behaves poorly inside layered top-level windows during resize/hit-testing.
                    // Keep the normal interactive window path as a regular window and enable transparent overlay mode only
                    // when click-through is explicitly requested.
                    var overlayMode = command.IsClickThrough ? OverlayMode.Transparent : OverlayMode.Normal;
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
                    var calculatedColor = command.BackgroundColor == Colors.Transparent
                        ? Colors.Transparent with {A = 1} //true transparent window is non-clickable
                        : command.BackgroundColor;
                    window.ApplyBackgroundColor(calculatedColor);
                    break;
                }
                case SetBorderColor command:
                {
                    Log.Debug($"Updating {nameof(BorderColor)} to {command.BorderColor}");
                    window.ApplyBorderColor(command.BorderColor);
                    break;
                }
                case SetBorderThickness command:
                {
                    Log.Debug($"Updating {nameof(BorderThickness)} to {command.BorderThickness}");
                    if (command.TitleBarDisplayMode is TitleBarDisplayMode.Custom or TitleBarDisplayMode.None)
                    {
                        window.BorderThickness = new Thickness(0);
                    }
                    else if (command.TitleBarDisplayMode == TitleBarDisplayMode.System)
                    {
                        window.BorderThickness = command.BorderThickness;
                        window.UpdateTitleBarDisplayMode(TitleBarDisplayMode.System);
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
                case SetBlazorUnityContainer command:
                {
                    Log.Debug($"Updating {nameof(Container)} to {command.ChildContainer}");
                    window.Container = command.ChildContainer;
                    break;
                }
                case SetBlazorFileProvider command:
                {
                    Log.Debug($"Updating {nameof(AdditionalFileProvider)} to {command.FileProvider}");
                    additionalFileProviderAnchor.Disposable = command.FileProvider == null
                        ? null
                        : compositeFileProvider.Add(command.FileProvider);
                    break;
                }
                case SetBlazorAdditionalFiles command:
                {
                    Log.Debug($"Updating {nameof(AdditionalFiles)} to {command.AdditionalFiles}");
                    window.BodyContentControl.AdditionalFiles = command.AdditionalFiles;
                    window.TitleBarContentControl.AdditionalFiles = command.AdditionalFiles;
                    break;
                }
                case SetBlazorControlConfigurator command:
                {
                    Log.Debug($"Updating {nameof(ControlConfigurator)} to {command.ControlConfigurator}");
                    window.BodyContentControl.Configurator = command.ControlConfigurator;
                    window.TitleBarContentControl.Configurator = command.ControlConfigurator;
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(windowEvent), $@"Unsupported event type: {windowEvent.GetType()}");
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

    private static IObservable<IWindowEvent> SubscribeToWindow(IFluentLog log, NativeWindow window, BlazorWindow blazorWindow)
    {
        return Observable.Create<IWindowEvent>(observer =>
        {
            log.Debug($"Creating new window and subscriptions");

            var anchors = new CompositeDisposable();
            Disposable.Create(() => log.Debug("Window subscription is being disposed")).AddTo(anchors);

            //SetupInitialState in Window is called BEFORE that SourceInitialized
            //so to set up proper initial size and position (for the very first frame)
            //we have to "guess" current DPI, then, when SourceInitialized will be called, it will be re-calculated again
            //best-case scenario - DPI won't change and there will be no blinking at all
            blazorWindow.HandleEvent(new SetShowInTaskbar(blazorWindow.ShowInTaskbar));
            blazorWindow.HandleEvent(new SetTopmostCommand(blazorWindow.Topmost));
            blazorWindow.HandleEvent(new SetAllowsTransparency(blazorWindow.AllowsTransparency));
            blazorWindow.HandleEvent(new SetBackgroundColor(blazorWindow.BackgroundColor));
            blazorWindow.HandleEvent(new SetBorderThickness(blazorWindow.TitleBarDisplayMode, blazorWindow.BorderThickness));
            blazorWindow.HandleEvent(new SetBorderColor(blazorWindow.BorderColor));
            blazorWindow.HandleEvent(new SetWindowPadding(blazorWindow.TitleBarDisplayMode, blazorWindow.Padding));
            blazorWindow.HandleEvent(new SetResizeMode(blazorWindow.ResizeMode));
            blazorWindow.HandleEvent(new SetWindowTitleCommand(blazorWindow.Title));
            blazorWindow.HandleEvent(new SetWindowState(blazorWindow.WindowState));
            blazorWindow.HandleEvent(new SetShowTitleBarCommand(
                blazorWindow.TitleBarDisplayMode,
                ShowCloseButton: blazorWindow.ShowCloseButton,
                ShowMinButton: blazorWindow.ShowMinButton,
                ShowMaxButton: blazorWindow.ShowMaxButton));
            blazorWindow.HandleEvent(new SetBlazorControlConfigurator(blazorWindow.ControlConfigurator));
            blazorWindow.HandleEvent(new SetBlazorAdditionalFiles(blazorWindow.AdditionalFiles));
            blazorWindow.HandleEvent(new SetBlazorFileProvider(blazorWindow.AdditionalFileProvider));
            blazorWindow.HandleEvent(new SetBlazorUnityContainer(blazorWindow.Container));
            UpdateWindowBoundsFromMonitor(IntPtr.Zero);

            EventHandler onSourceInitialized = (_, _) =>
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
                    blazorWindow.Log.Warn("Failed to reposition window on SourceInitialized", e);
                    throw;
                }
            };
            window.SourceInitialized += onSourceInitialized;
            Disposable.Create(() => window.SourceInitialized -= onSourceInitialized).AddTo(anchors);

            if (!window.IsHandleCreated)
            {
                _ = window.Handle;
            }

            EventHandler onLoaded = (_, _) =>
            {
                window.MessageHook += blazorWindow.WindowHook;
                Disposable.Create(() => window.MessageHook -= blazorWindow.WindowHook).AddTo(anchors);
            };
            window.Loaded += onLoaded;
            Disposable.Create(() => window.Loaded -= onLoaded).AddTo(anchors);

            //these events are internally using WPF window subsystem and probably should be moved to WindowHook
            EventHandler onStateChanged = (_, _) => observer.OnNext(new WindowStateChangedEvent(window.WindowState));
            window.StateChanged += onStateChanged;
            Disposable.Create(() => window.StateChanged -= onStateChanged).AddTo(anchors);

            //size/location-related events are handled in a special way - to avoid blinking, they are set BEFORE form is loaded
            blazorWindow.windowLeft
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowPosCommand(new Point()
                {
                    X = p.Value,
                    Y = blazorWindow.Top,
                })))
                .AddTo(anchors);

            blazorWindow.windowTop
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowPosCommand(new Point()
                {
                    X = blazorWindow.Left,
                    Y = p.Value,
                })))
                .AddTo(anchors);

            blazorWindow.windowWidth
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowSizeCommand(new Size()
                {
                    Width = p.Value,
                    Height = blazorWindow.Height
                })))
                .AddTo(anchors);

            blazorWindow.windowHeight
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(p => observer.OnNext(new SetWindowSizeCommand(new Size()
                {
                    Width = blazorWindow.Width,
                    Height = p.Value
                })))
                .AddTo(anchors);

            blazorWindow.windowTopmost
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetTopmostCommand(x.Value)))
                .AddTo(anchors);

            blazorWindow.windowTitle
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetWindowTitleCommand(x.Value)))
                .AddTo(anchors);

            blazorWindow.showInTaskbar
                .Listen()
                .Skip(1)
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetShowInTaskbar(x.Value)))
                .AddTo(anchors);

            Observable.CombineLatest(
                    blazorWindow.WhenAnyValue(x => x.TitleBarDisplayMode),
                    blazorWindow.WhenAnyValue(x => x.Padding),
                    (titleBarDisplayMode, borderThickness) =>
                        new SetWindowPadding(titleBarDisplayMode, borderThickness))
                .Skip(1)
                .Subscribe(x => observer.OnNext(x))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.BackgroundColor)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBackgroundColor(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.BorderColor)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBorderColor(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.AllowsTransparency)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetAllowsTransparency(x)))
                .AddTo(anchors);

            Observable.CombineLatest(
                    blazorWindow.WhenAnyValue(x => x.TitleBarDisplayMode),
                    blazorWindow.WhenAnyValue(x => x.BorderThickness),
                    (titleBarDisplayMode, borderThickness) =>
                        new SetBorderThickness(titleBarDisplayMode, borderThickness))
                .Skip(1)
                .Subscribe(x => observer.OnNext(x))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.ResizeMode)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetResizeMode(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.WindowState)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetWindowState(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.WindowStartupLocation)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetWindowStartupLocationCommand(x)))
                .AddTo(anchors);

            EventHandler onLoadedForDelayedProperties = (_, _) =>
            {
                //some properties could be set only AFTER window is loaded
                blazorWindow.WhenAnyValue(x => x.NoActivate)
                    .Subscribe(x => observer.OnNext(new SetNoActivate(x)))
                    .AddTo(anchors);

                //to avoid System.InvalidOperationException: Transparent mode requires AllowsTransparency to be set to True
                blazorWindow.WhenAnyValue(x => x.IsClickThrough)
                    .Subscribe(x => observer.OnNext(new SetIsClickThrough(x)))
                    .AddTo(anchors);

                blazorWindow.WhenAnyValue(x => x.Opacity)
                    .Subscribe(x => observer.OnNext(new SetOpacity(x)))
                    .AddTo(anchors);
            };
            window.Loaded += onLoadedForDelayedProperties;
            Disposable.Create(() => window.Loaded -= onLoadedForDelayedProperties).AddTo(anchors);

            Observable.CombineLatest(
                    blazorWindow.WhenAnyValue(x => x.TitleBarDisplayMode),
                    blazorWindow.WhenAnyValue(x => x.ShowCloseButton),
                    blazorWindow.WhenAnyValue(x => x.ShowMinButton),
                    blazorWindow.WhenAnyValue(x => x.ShowMaxButton),
                    (titleBarDisplayMode, showCloseButton, showMinButton, showMaxButton) =>
                        new SetShowTitleBarCommand(titleBarDisplayMode, ShowCloseButton: showCloseButton, ShowMinButton: showMinButton, ShowMaxButton: showMaxButton)
                )
                .Skip(1)
                .Subscribe(x => { observer.OnNext(x); })
                .AddTo(anchors);

            blazorWindow.windowVisible
                .Listen()
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetVisibleCommand(x.Value)))
                .AddTo(anchors);

            // blazor-related events
            blazorWindow.WhenAnyValue(x => x.Container)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBlazorUnityContainer(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.AdditionalFileProvider)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBlazorFileProvider(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.AdditionalFiles)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBlazorAdditionalFiles(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.ControlConfigurator)
                .Skip(1)
                .Subscribe(x => observer.OnNext(new SetBlazorControlConfigurator(x)))
                .AddTo(anchors);

            // events propagation
            SubscribeToInputEvents(window.BodyContentControl.WebView);
            SubscribeToInputEvents(window.TitleBarContentControl.WebView);

            EventHandler onWindowLoaded = (_, args) => blazorWindow.Loaded?.Invoke(blazorWindow, args);
            window.Loaded += onWindowLoaded;
            Disposable.Create(() => window.Loaded -= onWindowLoaded).AddTo(anchors);

            EventHandler onWindowUnloaded = (_, args) => blazorWindow.Unloaded?.Invoke(blazorWindow, args);
            window.Unloaded += onWindowUnloaded;
            Disposable.Create(() => window.Unloaded -= onWindowUnloaded).AddTo(anchors);

            EventHandler onWindowClosed = (_, args) =>
            {
                blazorWindow.Log.Debug($"Native Window has been closed");
                if (!window.IsDisposed)
                {
                    blazorWindow.Log.Debug($"Disposing native window");
                    window.Dispose();
                }

                blazorWindow.Closed?.Invoke(blazorWindow, args);
                if (!blazorWindow.isClosedTcs.TrySetResult())
                {
                    blazorWindow.Log.Debug($"Could not notify about window closure, tcs: {blazorWindow.isClosedTcs}");
                }
            };
            window.Closed += onWindowClosed;
            Disposable.Create(() => window.Closed -= onWindowClosed).AddTo(anchors);

            CancelEventHandler onWindowClosing = (_, args) => blazorWindow.Closing?.Invoke(blazorWindow, args);
            window.Closing += onWindowClosing;
            Disposable.Create(() => window.Closing -= onWindowClosing).AddTo(anchors);

            EventHandler onWindowActivated = (_, args) => blazorWindow.Activated?.Invoke(blazorWindow, args);
            window.Activated += onWindowActivated;
            Disposable.Create(() => window.Activated -= onWindowActivated).AddTo(anchors);

            EventHandler onWindowDeactivated = (_, args) => blazorWindow.Deactivated?.Invoke(blazorWindow, args);
            window.Deactivated += onWindowDeactivated;
            Disposable.Create(() => window.Deactivated -= onWindowDeactivated).AddTo(anchors);

            if (!blazorWindow.Anchors.IsDisposed)
            {
                var windowHandle = window.WindowHandle;
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

            void SubscribeToInputEvents(BlazorWebViewEx inputEventSource)
            {
                var activeInputAnchor = new SerialDisposable().AddTo(anchors);

                void AttachToControl(Microsoft.Web.WebView2.WinForms.WebView2 control)
                {
                    var inputAnchors = new CompositeDisposable();

                    MouseEventHandlerForms onMouseDown = (_, args) =>
                    {
                        blazorWindow.MouseDown?.Invoke(blazorWindow, CreateMouseButtonEventArgs(args, isPreview: false, isMouseUp: false));
                        blazorWindow.PreviewMouseDown?.Invoke(blazorWindow, CreateMouseButtonEventArgs(args, isPreview: true, isMouseUp: false));
                    };
                    control.MouseDown += onMouseDown;
                    Disposable.Create(() => control.MouseDown -= onMouseDown).AddTo(inputAnchors);

                    MouseEventHandlerForms onMouseUp = (_, args) =>
                    {
                        blazorWindow.MouseUp?.Invoke(blazorWindow, CreateMouseButtonEventArgs(args, isPreview: false, isMouseUp: true));
                        blazorWindow.PreviewMouseUp?.Invoke(blazorWindow, CreateMouseButtonEventArgs(args, isPreview: true, isMouseUp: true));
                    };
                    control.MouseUp += onMouseUp;
                    Disposable.Create(() => control.MouseUp -= onMouseUp).AddTo(inputAnchors);

                    MouseEventHandlerForms onMouseMove = (_, args) =>
                    {
                        blazorWindow.MouseMove?.Invoke(blazorWindow, CreateMouseEventArgs(args, isPreview: false));
                        blazorWindow.PreviewMouseMove?.Invoke(blazorWindow, CreateMouseEventArgs(args, isPreview: true));
                    };
                    control.MouseMove += onMouseMove;
                    Disposable.Create(() => control.MouseMove -= onMouseMove).AddTo(inputAnchors);

                    PreviewKeyDownEventHandlerForms onPreviewKeyDown = (_, args) => { args.IsInputKey = true; };
                    control.PreviewKeyDown += onPreviewKeyDown;
                    Disposable.Create(() => control.PreviewKeyDown -= onPreviewKeyDown).AddTo(inputAnchors);

                    KeyEventHandlerForms onKeyDown = (_, args) =>
                    {
                        var previewKeyDownArgs = CreateKeyEventArgs(args, Keyboard.PreviewKeyDownEvent);
                        blazorWindow.PreviewKeyDown?.Invoke(blazorWindow, previewKeyDownArgs);
                        if (previewKeyDownArgs.Handled)
                        {
                            args.Handled = true;
                            args.SuppressKeyPress = true;
                            return;
                        }

                        var keyDownArgs = CreateKeyEventArgs(args, Keyboard.KeyDownEvent);
                        blazorWindow.KeyDown?.Invoke(blazorWindow, keyDownArgs);
                        if (keyDownArgs.Handled)
                        {
                            args.Handled = true;
                            args.SuppressKeyPress = true;
                        }
                    };
                    control.KeyDown += onKeyDown;
                    Disposable.Create(() => control.KeyDown -= onKeyDown).AddTo(inputAnchors);

                    KeyEventHandlerForms onKeyUp = (_, args) =>
                    {
                        var previewArgs = CreateKeyEventArgs(args, Keyboard.PreviewKeyUpEvent);
                        blazorWindow.PreviewKeyUp?.Invoke(blazorWindow, previewArgs);
                        if (previewArgs.Handled)
                        {
                            args.Handled = true;
                            return;
                        }

                        var keyUpArgs = CreateKeyEventArgs(args, Keyboard.KeyUpEvent);
                        blazorWindow.KeyUp?.Invoke(blazorWindow, keyUpArgs);
                        if (keyUpArgs.Handled)
                        {
                            args.Handled = true;
                        }
                    };
                    control.KeyUp += onKeyUp;
                    Disposable.Create(() => control.KeyUp -= onKeyUp).AddTo(inputAnchors);

                    KeyPressEventHandlerForms onKeyPress = (_, _) => { };
                    control.KeyPress += onKeyPress;
                    Disposable.Create(() => control.KeyPress -= onKeyPress).AddTo(inputAnchors);

                    activeInputAnchor.Disposable = inputAnchors;
                }

                EventHandler<BlazorWebViewInitializedEventArgs> onBlazorWebViewInitialized = (_, args) => AttachToControl(args.WebView);
                inputEventSource.BlazorWebViewInitialized += onBlazorWebViewInitialized;
                Disposable.Create(() => inputEventSource.BlazorWebViewInitialized -= onBlazorWebViewInitialized).AddTo(anchors);

                AttachToControl(inputEventSource.WebView);
            }
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
                    PInvoke.SHCore.GetDpiForMonitor(desktopMonitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY);
                if (dpiResult.Failed)
                {
                    log.Warn($"Failed to GetDpiForMonitor and set initial window position, hrResult: {dpiResult}");
                    return;
                }

                SetWindowStartupLocation(hwnd, blazorWindow.WindowStartupLocation);
                AssignWindowBounds(scaleX: 96d / dpiX, scaleY: 96d / dpiY);
            }
            catch (Exception e)
            {
                log.Warn("Failed to set initial window position", e);
            }
        }

        void AssignWindowBounds(double scaleX, double scaleY)
        {
            var desiredWidth = blazorWindow.Width * scaleX;
            if (!double.IsFinite(window.Width) || Math.Abs(desiredWidth - window.Width) > 0.5)
            {
                window.Width = (int) Math.Round(desiredWidth);
            }

            var desiredHeight = blazorWindow.Height * scaleY;
            if (!double.IsFinite(window.Height) || Math.Abs(desiredHeight - window.Height) > 0.5)
            {
                window.Height = (int) Math.Round(desiredHeight);
            }

            var desiredLeft = blazorWindow.Left * scaleX;
            if (!double.IsFinite(window.Left) || Math.Abs(desiredLeft - window.Left) > 0.5)
            {
                window.Left = (int) Math.Round(desiredLeft);
            }

            var desiredTop = blazorWindow.Top * scaleY;
            if (!double.IsFinite(window.Top) || Math.Abs(desiredTop - window.Top) > 0.5)
            {
                window.Top = (int) Math.Round(desiredTop);
            }
        }

        void CenterWindowWithin(Rectangle monitorBounds)
        {
            var left = monitorBounds.X + (monitorBounds.Width - blazorWindow.Width) / 2;
            if (left != blazorWindow.Left)
            {
                blazorWindow.Left = left;
            }

            var top = monitorBounds.Y + (monitorBounds.Height - blazorWindow.Height) / 2;
            if (top != blazorWindow.Top)
            {
                blazorWindow.Top = top;
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
                        blazorWindow.Log.Warn($"Failed to set initial window size - could not find desktop monitor");
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
                    IntPtr owner;
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        owner = currentProcess.MainWindowHandle;
                    }
                    catch (Exception e)
                    {
                        log.Warn("Failed to find main window of the current process", e);
                        owner = IntPtr.Zero;
                    }

                    if (owner == IntPtr.Zero)
                    {
                        log.Warn("Owner handle is not set, centering within screen");
                        SetWindowStartupLocation(hwnd, WindowStartupLocation.CenterScreen);
                        return;
                    }

                    if (!User32.GetWindowRect(owner, out var windowRect))
                    {
                        log.Warn($"Failed to set initial window size - could not get rect of owner window {owner.ToHexadecimal()}");
                        break;
                    }

                    var windowBounds = Rectangle.FromLTRB(windowRect.left, windowRect.top, windowRect.right, windowRect.bottom);
                    log.Debug($"Centering window within window bounds {windowBounds}");
                    CenterWindowWithin(windowBounds);
                    break;
                }
            }
        }
    }


    private IntPtr WindowHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        try
        {
            var msg = (User32.WindowMessage) msgRaw;
            switch (msg)
            {
                case User32.WindowMessage.WM_SHOWWINDOW:
                    var isVisible = wParam != IntPtr.Zero;
                    EnqueueUpdate(new IsVisibleChangedEvent(isVisible));
                    break;
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
                    var x = lParam.LoWord();
                    var y = lParam.HiWord();
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

    private static MouseButtonEventArgs CreateMouseButtonEventArgs(MouseEventArgsForms args, bool isPreview, bool isMouseUp)
    {
        var button = args.Button switch
        {
            MouseButtonsForms.Left => MouseButton.Left,
            MouseButtonsForms.Middle => MouseButton.Middle,
            MouseButtonsForms.Right => MouseButton.Right,
            MouseButtonsForms.XButton1 => MouseButton.XButton1,
            MouseButtonsForms.XButton2 => MouseButton.XButton2,
            _ => MouseButton.Left
        };
        var eventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, button)
        {
            RoutedEvent = isPreview
                ? (isMouseUp ? Mouse.PreviewMouseUpEvent : Mouse.PreviewMouseDownEvent)
                : (isMouseUp ? Mouse.MouseUpEvent : Mouse.MouseDownEvent)
        };
        return eventArgs;
    }

    private static MouseEventArgs CreateMouseEventArgs(MouseEventArgsForms args, bool isPreview)
    {
        var eventArgs = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = isPreview ? Mouse.PreviewMouseMoveEvent : Mouse.MouseMoveEvent
        };
        return eventArgs;
    }

    private static KeyEventArgs CreateKeyEventArgs(KeyEventArgsForms args, RoutedEvent routedEvent)
    {
        return CreateKeyEventArgs((int) args.KeyCode, routedEvent);
    }

    private static KeyEventArgs CreateKeyEventArgs(PreviewKeyDownEventArgsForms args, RoutedEvent routedEvent)
    {
        return CreateKeyEventArgs((int) args.KeyCode, routedEvent);
    }

    private static KeyEventArgs CreateKeyEventArgs(int virtualKey, RoutedEvent routedEvent)
    {
        var eventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, WinFormsPresentationSource.Instance, Environment.TickCount, KeyInterop.KeyFromVirtualKey(virtualKey))
        {
            RoutedEvent = routedEvent
        };
        return eventArgs;
    }

    private void ApplyWindowStartupLocation(NativeWindow window, WindowStartupLocation startupLocation)
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
        try
        {
            ownerHandle = Process.GetCurrentProcess().MainWindowHandle;
        }
        catch (Exception e)
        {
            Log.Warn("Failed to find main window of the current process for startup location update", e);
            ownerBounds = default;
            return false;
        }

        if (ownerHandle == IntPtr.Zero)
        {
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

    private sealed record WaitForIdleCommand(ManualResetEventSlim ResetEvent, DateTimeOffset Timestamp) : IWindowCommand;

    private sealed record InvokeCommand(Action ActionToExecute, ManualResetEventSlim ResetEvent, DateTimeOffset Timestamp) : IWindowCommand;

    private sealed record ActivateCommand : IWindowCommand;

    private sealed record ShowDevToolsCommand : IWindowCommand;

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

    private sealed record SetIsClickThrough(bool IsClickThrough) : IWindowCommand;

    private sealed record SetOpacity(double Opacity) : IWindowCommand;

    private sealed record SetBackgroundColor(Color BackgroundColor) : IWindowCommand;

    private sealed record SetBorderColor(Color BorderColor) : IWindowCommand;

    private sealed record SetBlazorUnityContainer(IUnityContainer ChildContainer) : IWindowCommand;

    private sealed record SetBlazorFileProvider(IFileProvider FileProvider) : IWindowCommand;

    private sealed record SetBlazorControlConfigurator(IBlazorContentControlConfigurator ControlConfigurator) : IWindowCommand;

    private sealed record SetBlazorAdditionalFiles(ImmutableArray<IFileInfo> AdditionalFiles) : IWindowCommand;

    private sealed record IsVisibleChangedEvent(bool IsVisible) : IWindowEvent;

    private sealed record WindowTitleChangedEvent(string Title) : IWindowEvent;

    private sealed record WindowPosChangedEvent(Point Location) : IWindowEvent;

    private sealed record WindowSizeChangedEvent(Size Size) : IWindowEvent;

    private sealed record WindowStateChangedEvent(WindowState WindowState) : IWindowEvent;

    private interface IWindowCommand : IWindowEvent
    {
    }

    private interface IWindowEvent
    {
    }
}
