using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.FileProviders;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI;
using ReactiveUI;
using Unity;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorWindow : DisposableReactiveObjectWithLogger, IBlazorWindow
{
    private static readonly Color DefaultBackgroundColor = Color.FromArgb(0xFF, 0x42, 0x42, 0x42);

    private readonly IUnityContainer unityContainer;
    private readonly Lazy<NativeWindow> windowSupplier;

    private readonly long windowId = BlazorWindowCounter.GetNext();

    private readonly PropertyValueHolder<int> windowLeft;
    private readonly PropertyValueHolder<int> windowWidth;
    private readonly PropertyValueHolder<int> windowTop;
    private readonly PropertyValueHolder<int> windowHeight;
    private readonly PropertyValueHolder<string> windowTitle;

    private readonly PropertyValueHolder<int> windowMinWidth;
    private readonly PropertyValueHolder<int> windowMinHeight;
    private readonly PropertyValueHolder<int> windowMaxWidth;
    private readonly PropertyValueHolder<int> windowMaxHeight;
    private readonly PropertyValueHolder<bool> windowTopmost;
    private readonly PropertyValueHolder<bool> windowVisible;
    private readonly PropertyValueHolder<bool> showInTaskbar;

    private readonly BlockingCollection<IWindowEvent> eventQueue;
    private readonly IScheduler uiScheduler;
    private readonly ManualResetEventSlim isClosed = new(false);

    public BlazorWindow(
        IUnityContainer unityContainer, 
        [OptionalDependency] IScheduler uiScheduler = default)
    {
        Log.AddSuffix($"BWnd#{windowId}");
        Log.Debug("New window is created");
        this.unityContainer = unityContainer;

        this.uiScheduler = uiScheduler ?? SchedulerProvider.Instance.GetOrAdd("BlazorWindow");
        windowSupplier = new Lazy<NativeWindow>(() => CreateWindow());
        eventQueue = new BlockingCollection<IWindowEvent>();

        Disposable.Create(() =>
        {
            try
            {
                isClosed.Set();

                if (!windowSupplier.IsValueCreated)
                {
                    return;
                }

                var window = windowSupplier.Value;
                if (window.Anchors.IsDisposed)
                {
                    return;
                }

                EnqueueUpdate(new DisposeWindowCommand());
            }
            catch (Exception e)
            {
                Log.Warn("Failed to dispose window", e);
            }
        }).AddTo(Anchors);

        windowLeft = new PropertyValueHolder<int>(this, nameof(Left)).AddTo(Anchors);
        windowTop = new PropertyValueHolder<int>(this, nameof(Top)).AddTo(Anchors);
        windowWidth = new PropertyValueHolder<int>(this, nameof(Width)).AddTo(Anchors);
        windowHeight = new PropertyValueHolder<int>(this, nameof(Height)).AddTo(Anchors);
        windowTitle = new PropertyValueHolder<string>(this, nameof(Title)).AddTo(Anchors);
        windowMinWidth = new PropertyValueHolder<int>(this, nameof(MinWidth)).AddTo(Anchors);
        windowMinHeight = new PropertyValueHolder<int>(this, nameof(MinHeight)).AddTo(Anchors);
        windowMaxWidth = new PropertyValueHolder<int>(this, nameof(MaxWidth)).AddTo(Anchors);
        windowMaxHeight = new PropertyValueHolder<int>(this, nameof(MaxHeight)).AddTo(Anchors);
        windowTopmost = new PropertyValueHolder<bool>(this, nameof(Topmost)).AddTo(Anchors);
        windowVisible = new PropertyValueHolder<bool>(this, nameof(IsVisible)).AddTo(Anchors);
        showInTaskbar = new PropertyValueHolder<bool>(this, nameof(ShowInTaskbar)).AddTo(Anchors);

        ShowInTaskbar = true;
        Width = 300;
        Height = 200;
        ShowMinButton = true;
        ShowMaxButton = true;
        ShowCloseButton = true;

        WhenKeyDown =
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => KeyDown += h, h => KeyDown -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenKeyUp =
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => KeyUp += h, h => KeyUp -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenPreviewKeyDown =
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => PreviewKeyDown += h, h => PreviewKeyDown -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenPreviewKeyUp =
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => PreviewKeyUp += h, h => PreviewKeyUp -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        var whenLoadedSource = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => Loaded += h, h => Loaded -= h)
            .Select(x => x.EventArgs)
            .Take(1)
            .Replay(1);
        whenLoadedSource.Connect().AddTo(Anchors);
        WhenLoaded = whenLoadedSource;

        var whenClosedSource = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => Closed += h, h => Closed -= h)
            .Select(x => x.EventArgs)
            .Take(1)
            .Replay(1);
        whenClosedSource.Connect().AddTo(Anchors);
        WhenClosed = whenClosedSource;

        WhenClosing =
            Observable
                .FromEventPattern<CancelEventHandler, CancelEventArgs>(h => Closing += h, h => Closing -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenActivated =
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => Activated += h, h => Activated -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenDeactivated =
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => Deactivated += h, h => Deactivated -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenClosed.Subscribe(() =>
        {
            if (Anchors.IsDisposed)
            {
                return;
            }

            Log.Debug("Got Closed signal, disposing");
            Dispose();
        }).AddTo(Anchors);
    }

    public Type ViewType { get; set; }

    public object ViewDataContext { get; set; }

    public IUnityContainer Container { get; set; }

    public WindowStartupLocation WindowStartupLocation { get; set; }

    public ResizeMode ResizeMode { get; set; } = ResizeMode.CanResizeWithGrip;

    public TitleBarDisplayMode TitleBarDisplayMode { get; set; }

    public bool ShowInTaskbar
    {
        get => showInTaskbar.State.Value;
        set => showInTaskbar.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public Thickness Padding { get; set; } = new(2);
    public Thickness BorderThickness { get; set; }

    public bool IsClickThrough { get; set; }

    public bool IsDebugMode { get; set; }

    public bool ShowCloseButton { get; set; }

    public bool ShowMinButton { get; set; }

    public bool ShowMaxButton { get; set; }

    public double Opacity { get; set; } = 1;

    public Color BackgroundColor { get; set; } = DefaultBackgroundColor;

    public Color BorderColor { get; set; }

    public bool IsVisible
    {
        get => windowVisible.State.Value;
        set => windowVisible.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public string Title
    {
        get => windowTitle.State.Value;
        set => windowTitle.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public bool Topmost
    {
        get => windowTopmost.State.Value;
        set => windowTopmost.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int Left
    {
        get => windowLeft.State.Value;
        set => windowLeft.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int Top
    {
        get => windowTop.State.Value;
        set => windowTop.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int Width
    {
        get => windowWidth.State.Value;
        set => windowWidth.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int Height
    {
        get => windowHeight.State.Value;
        set => windowHeight.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int MinWidth
    {
        get => windowMinWidth.State.Value;
        set => windowMinWidth.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int MinHeight
    {
        get => windowMinHeight.State.Value;
        set => windowMinHeight.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int MaxWidth
    {
        get => windowMaxWidth.State.Value;
        set => windowMaxWidth.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public int MaxHeight
    {
        get => windowMaxHeight.State.Value;
        set => windowMaxHeight.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public ImmutableArray<IFileInfo> AdditionalFiles { get; set; } = ImmutableArray<IFileInfo>.Empty;
    
    public IFileProvider AdditionalFileProvider { get; set; }

    public IObservable<KeyEventArgs> WhenKeyDown { get; }
    public IObservable<KeyEventArgs> WhenKeyUp { get; }
    public IObservable<KeyEventArgs> WhenPreviewKeyDown { get; }
    public IObservable<KeyEventArgs> WhenPreviewKeyUp { get; }
    public IObservable<EventArgs> WhenLoaded { get; }
    public IObservable<EventArgs> WhenClosed { get; }
    public IObservable<CancelEventArgs> WhenClosing { get; }
    public IObservable<EventArgs> WhenActivated { get; }
    public IObservable<EventArgs> WhenDeactivated { get; }

    public event KeyEventHandler KeyDown;
    public event KeyEventHandler KeyUp;
    public event KeyEventHandler PreviewKeyDown;
    public event KeyEventHandler PreviewKeyUp;
    public event CancelEventHandler Closing;
    public event EventHandler Activated;
    public event EventHandler Deactivated;
    public event EventHandler Loaded;
    public event EventHandler Closed;
    
    public void ShowDevTools()
    {
        Log.Debug("Enqueueing ShowDevTools command");
        EnqueueUpdate(new ShowDevToolsCommand());
    }

    public void Hide()
    {
        Log.Debug("Enqueueing Hide command");
        EnqueueUpdate(new HideCommand());
    }

    public void Show()
    {
        EnsureNotDisposed();
        EnqueueUpdate(new ShowCommand());

        Log.Debug("Showing window in non-blocking way");
    }

    public void ShowDialog(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        Log.Debug("Showing window in blocking way");
        EnqueueUpdate(new ShowCommand());
        try
        {
            Log.Debug("Awaiting for the window to be closed");
            isClosed.Wait(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Wait was cancelled");
        }
    }

    public void Close()
    {
        EnsureNotDisposed();
        Log.Debug("Closing the window");
        EnqueueUpdate(new CloseCommand());
    }

    public IntPtr GetWindowHandle()
    {
        EnsureNotDisposed();
        if (!windowSupplier.IsValueCreated)
        {
            throw new InvalidOperationException("Window is not created yet");
        }

        var window = windowSupplier.Value;
        return window.WindowHandle;
    }

    public void SetWindowRect(Rectangle rect)
    {
        EnsureNotDisposed();
        Log.Debug($"Setting window rect to {rect}");
        EnqueueUpdate(new SetWindowRectCommand(rect));
    }

    public void SetWindowSize(Size windowSize)
    {
        EnsureNotDisposed();
        Log.Debug($"Resizing window to {windowSize}");
        EnqueueUpdate(new SetWindowSizeCommand(windowSize));
    }

    public void SetWindowPos(Point windowPos)
    {
        EnsureNotDisposed();
        Log.Debug($"Moving window to {windowPos}");
        EnqueueUpdate(new SetWindowPosCommand(windowPos));
    }

    private void EnqueueUpdate(IWindowEvent windowEvent)
    {
        eventQueue.Add(windowEvent);
        EnqueueUpdate().AndForget();
    }

    private Task EnqueueUpdate()
    {
        return Observable.Start(HandleUpdate, uiScheduler).ToTask();
    }

    private NativeWindow GetOrCreate()
    {
        NativeWindow window;
        if (!windowSupplier.IsValueCreated)
        {
            window = windowSupplier.Value;
            SubscribeToWindow(Log, window, this)
                .Subscribe(x => { EnqueueUpdate(x); })
                .AddTo(Anchors);

            Log.Debug("NativeWindow created and subscribed successfully");
        }
        else
        {
            window = windowSupplier.Value;
        }

        return window;
    }

    private void HandleUpdate()
    {
        uiScheduler.EnsureOnScheduler();

        if (eventQueue.Count <= 0)
        {
            return;
        }

        while (eventQueue.TryTake(out var windowEvent))
        {
            if (windowEvent is DisposeWindowCommand)
            {
                if (!windowSupplier.IsValueCreated)
                {
                    Log.Debug($"Window is not created - ignoring disposal request");
                    continue;
                }

                var window = GetOrCreate();
                if (window.Anchors.IsDisposed)
                {
                    Log.Debug($"Window already disposed - ignoring disposal request");
                    continue;
                }

                Log.Debug($"Disposing the window: {new { window }}");
                window.Close();
                window.Dispose();
            }
            else if (windowEvent is IWindowCommand)
            {
                if (Anchors.IsDisposed)
                {
                    Log.Debug($"Ignoring command - already disposed, command: {windowEvent}");
                    continue;
                }

                var window = GetOrCreate();
                switch (windowEvent)
                {
                    case SetVisibleCommand command:
                    {
                        Log.Debug($"Updating {nameof(IsVisible)} to {command.IsVisible}");
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
                    case ShowCommand:
                    {
                        Log.Debug($"Showing the window: {new { window.WindowState }}");
                        window.Show();
                        break;
                    }
                    case HideCommand:
                    {
                        Log.Debug($"Hiding the window: {new { window.WindowState }}");
                        window.Hide();
                        break;
                    } 
                    case ShowDevToolsCommand:
                    {
                        Log.Debug($"Showing dev tools");
                        window.ContentControl.OpenDevTools().AndForget();
                        break;
                    }
                    case CloseCommand:
                    {
                        Log.Debug($"Closing the window: {new { window.WindowState }}");
                        window.Close();
                        break;
                    }
                    case SetWindowTitleCommand command:
                    {
                        Log.Debug($"Updating {nameof(window.Title)} to {command.Title}");
                        window.Title = command.Title;
                        break;
                    }
                    case SetWindowPosCommand command:
                    {
                        Log.Debug($"Setting window position to {command.Location}");
                        UnsafeNative.SetWindowPos(window.WindowHandle, command.Location);
                        break;
                    }
                    case SetWindowRectCommand command:
                    {
                        Log.Debug($"Setting window rect to {command.Rect}");
                        UnsafeNative.SetWindowRect(window.WindowHandle, command.Rect);
                        break;
                    }
                    case SetWindowSizeCommand command:
                    {
                        Log.Debug($"Setting window size to {command.Size}");
                        UnsafeNative.SetWindowSize(window.WindowHandle, command.Size);
                        break;
                    }
                    case SetShowTitleBarCommand command:
                    {
                        Log.Debug($"Updating {nameof(TitleBarDisplayMode)} to {command.TitleBarDisplayMode}");

                        var displayMode = command.TitleBarDisplayMode == TitleBarDisplayMode.Default
                            ? TitleBarDisplayMode.System
                            : command.TitleBarDisplayMode;

                        var showSystemBar = displayMode is TitleBarDisplayMode.System;
                        window.ShowTitleBar = showSystemBar;
                        window.ShowSystemMenu = showSystemBar;
                        window.ShowSystemMenuOnRightClick = showSystemBar;
                        window.ShowMinButton = showSystemBar && command.ShowMinButton;
                        window.ShowMaxRestoreButton = showSystemBar && command.ShowMaxButton;
                        window.ShowCloseButton = showSystemBar && command.ShowCloseButton;
                        break;
                    }
                    case SetWindowPadding command:
                    {
                        Log.Debug($"Updating {nameof(Padding)} to {command.Padding}");
                        window.ContentControl.Margin = command.Padding;
                        break;
                    }
                    case SetResizeMode command:
                    {
                        Log.Debug($"Updating {nameof(ResizeMode)} to {command.ResizeMode}");
                        window.ResizeMode = command.ResizeMode;
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
                        var overlayMode = command.IsClickThrough ? OverlayMode.Transparent : OverlayMode.Layered;
                        Log.Debug($"Updating OverlayMode to {overlayMode}");
                        window.SetOverlayMode(overlayMode);
                        break;
                    }
                    case SetOpacity command:
                    {
                        Log.Debug($"Updating {nameof(Opacity)} to {command.Opacity}");
                        window.Opacity = command.Opacity;
                        break;
                    }
                    case SetBackgroundColor command:
                    {
                        Log.Debug($"Updating {nameof(BackgroundColor)} to {command.BackgroundColor}");
                        var color = new SolidColorBrush(command.BackgroundColor);
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
                        window.BorderThickness = command.BorderThickness;
                        break;
                    }
                    case SetTopmostCommand command:
                    {
                        Log.Debug($"Updating {nameof(Topmost)} to {command.Topmost}");
                        window.Topmost = command.Topmost;
                        break;
                    }
                    case SetBlazorUnityContainer command:
                    {
                        Log.Debug($"Updating {nameof(Container)} to {command.ChildContainer}");
                        window.ContentControl.Container = command.ChildContainer;
                        break;
                    }
                    case SetBlazorFileProvider command:
                    {
                        Log.Debug($"Updating {nameof(AdditionalFileProvider)} to {command.FileProvider}");
                        window.ContentControl.AdditionalFileProvider = command.FileProvider;
                        break;
                    } 
                    case SetBlazorAdditionalFiles command:
                    {
                        Log.Debug($"Updating {nameof(AdditionalFiles)} to {command.AdditionalFiles}");
                        window.ContentControl.AdditionalFiles = command.AdditionalFiles;
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
                    continue;
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
                    default: throw new ArgumentOutOfRangeException(nameof(windowEvent), $@"Unsupported event type: {windowEvent.GetType()}");
                }
            }
        }
    }

    private static IObservable<IWindowEvent> SubscribeToWindow(IFluentLog log, NativeWindow window, BlazorWindow blazorWindow)
    {
        return Observable.Create<IWindowEvent>(observer =>
        {
            var anchors = new CompositeDisposable();
            Disposable.Create(() => log.Debug("Window subscription is being disposed")).AddTo(anchors);

            //SetupInitialState in Window is called BEFORE that SourceInitialized
            //so to set up proper initial size and position (for the very first frame)
            //we have to "guess" current DPI, then, when SourceInitialized will be called, it will be re-calculated again
            //best-case scenario - DPI won't change and there will be no blinking at all
            window.Topmost = blazorWindow.Topmost;
            window.ShowInTaskbar = blazorWindow.ShowInTaskbar;
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
                        blazorWindow.Log.Warn("Failed to reposition window on SourceInitialized", e);
                        throw;
                    }
                })
                .AddTo(anchors);

            window.WhenLoaded()
                .Subscribe(() =>
                {
                    try
                    {
                        var source = (HwndSource)PresentationSource.FromVisual(window);
                        if (source == null)
                        {
                            throw new InvalidOperationException("HwndSource must be initialized at this point");
                        }

                        source.AddHook(blazorWindow.WindowHook);
                        Disposable.Create(() =>
                        {
                            try
                            {
                                source.RemoveHook(blazorWindow.WindowHook);
                            }
                            catch (Exception e)
                            {
                                blazorWindow.Log.Warn("Failed to remove window hook", e);
                            }
                        }).AddTo(anchors);
                    }
                    catch (Exception e)
                    {
                        blazorWindow.Log.Warn("Failed to add window hook", e);
                        throw;
                    }
                })
                .AddTo(anchors);

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
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetTopmostCommand(x.Value)))
                .AddTo(anchors);

            blazorWindow.windowTitle
                .Listen()
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetWindowTitleCommand(x.Value)))
                .AddTo(anchors);

            blazorWindow.showInTaskbar
                .Listen()
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetShowInTaskbar(x.Value)))
                .AddTo(anchors);

            blazorWindow.windowVisible
                .Listen()
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.External)
                .Subscribe(x => observer.OnNext(new SetVisibleCommand(x.Value)))
                .AddTo(anchors);
            
            blazorWindow.WhenAnyValue(x => x.Container)
                .Subscribe(x => observer.OnNext(new SetBlazorUnityContainer(x)))
                .AddTo(anchors);
            
            blazorWindow.WhenAnyValue(x => x.AdditionalFileProvider)
                .Subscribe(x => observer.OnNext(new SetBlazorFileProvider(x)))
                .AddTo(anchors);  
            
            blazorWindow.WhenAnyValue(x => x.AdditionalFiles)
                .Subscribe(x => observer.OnNext(new SetBlazorAdditionalFiles(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.Padding)
                .Subscribe(x => observer.OnNext(new SetWindowPadding(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.BackgroundColor)
                .Subscribe(x => observer.OnNext(new SetBackgroundColor(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.BorderColor)
                .Subscribe(x => observer.OnNext(new SetBorderColor(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.BorderThickness)
                .Subscribe(x => observer.OnNext(new SetBorderThickness(x)))
                .AddTo(anchors);

            blazorWindow.WhenAnyValue(x => x.ResizeMode)
                .Subscribe(x => observer.OnNext(new SetResizeMode(x)))
                .AddTo(anchors);

            window.WhenLoaded()
                .Subscribe(() =>
                {
                    //to avoid System.InvalidOperationException: Transparent mode requires AllowsTransparency to be set to True
                    blazorWindow.WhenAnyValue(x => x.IsClickThrough)
                        .Subscribe(x => observer.OnNext(new SetIsClickThrough(x)))
                        .AddTo(anchors);

                    blazorWindow.WhenAnyValue(x => x.Opacity)
                        .Subscribe(x => observer.OnNext(new SetOpacity(x)))
                        .AddTo(anchors);
                })
                .AddTo(anchors);

            Observable.CombineLatest(
                    blazorWindow.WhenAnyValue(x => x.TitleBarDisplayMode),
                    blazorWindow.WhenAnyValue(x => x.ShowCloseButton),
                    blazorWindow.WhenAnyValue(x => x.ShowMinButton),
                    blazorWindow.WhenAnyValue(x => x.ShowMaxButton),
                    (titleBarDisplayMode, showCloseButton, showMinButton, showMaxButton) =>
                        new SetShowTitleBarCommand(titleBarDisplayMode, ShowCloseButton: showCloseButton, ShowMinButton: showMinButton, ShowMaxButton: showMaxButton)
                )
                .Subscribe(x => { observer.OnNext(x); })
                .AddTo(anchors);

            // events propagation
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.ContentControl.WebView.KeyDown += h, h => window.ContentControl.WebView.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.KeyDown?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.ContentControl.WebView.KeyUp += h, h => window.ContentControl.WebView.KeyUp -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.KeyUp?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.ContentControl.WebView.PreviewKeyDown += h, h => window.ContentControl.WebView.PreviewKeyDown -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.PreviewKeyDown?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.ContentControl.WebView.PreviewKeyUp += h, h => window.ContentControl.WebView.PreviewKeyUp -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.PreviewKeyUp?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.Loaded?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    blazorWindow.Closed?.Invoke(blazorWindow, x);
                    blazorWindow.isClosed.Set();
                })
                .AddTo(anchors);

            Observable
                .FromEventPattern<CancelEventHandler, CancelEventArgs>(h => window.Closing += h, h => window.Closing -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.Closing?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Activated += h, h => window.Activated -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.Activated?.Invoke(blazorWindow, x))
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Deactivated += h, h => window.Deactivated -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x => blazorWindow.Deactivated?.Invoke(blazorWindow, x))
                .AddTo(anchors);

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
                window.Width = desiredWidth;
            }

            var desiredHeight = blazorWindow.Height * scaleY;
            if (!double.IsFinite(window.Height) || Math.Abs(desiredHeight - window.Height) > 0.5)
            {
                window.Height = desiredHeight;
            }

            var desiredLeft = blazorWindow.Left * scaleX;
            if (!double.IsFinite(window.Left) || Math.Abs(desiredLeft -  window.Left) > 0.5)
            {
                window.Left = desiredLeft;
            }
            
            var desiredTop = blazorWindow.Top * scaleY;
            if (!double.IsFinite(window.Top) || Math.Abs(desiredTop -  window.Top) > 0.5)
            {
                window.Top = desiredTop;
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

                    if (!User32.GetWindowRect(desktopMonitor, out var monitorRect))
                    {
                        log.Warn($"Failed to set initial window size - could not get rect of desktop monitor {desktopMonitor.ToHexadecimal()}");
                        break;
                    }

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
                        break;
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

    private NativeWindow CreateWindow()
    {
        uiScheduler.EnsureOnScheduler();

        var window = new NativeWindow(this)
        {
            WindowStartupLocation = WindowStartupLocation,
        };

        //do not add window to Anchors! It must be disposed by DisposeWindow command
        return window;
    }

    private IntPtr WindowHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        try
        {
            var msg = (User32.WindowMessage)msgRaw;
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

    private sealed record ShowCommand : IWindowCommand;

    private sealed record HideCommand : IWindowCommand;
    
    private sealed record ShowDevToolsCommand : IWindowCommand;

    private sealed record CloseCommand : IWindowCommand;

    private sealed record DisposeWindowCommand : IWindowCommand;

    private sealed record SetWindowTitleCommand(string Title) : IWindowCommand;

    private sealed record SetWindowRectCommand(Rectangle Rect) : IWindowCommand;

    private sealed record SetWindowPosCommand(Point Location) : IWindowCommand;

    private sealed record SetWindowSizeCommand(Size Size) : IWindowCommand;

    private sealed record SetTopmostCommand(bool Topmost) : IWindowCommand;

    private sealed record SetVisibleCommand(bool IsVisible) : IWindowCommand;

    private sealed record SetWindowPadding(Thickness Padding) : IWindowCommand;

    private sealed record SetResizeMode(ResizeMode ResizeMode) : IWindowCommand;

    private sealed record SetShowTitleBarCommand(TitleBarDisplayMode TitleBarDisplayMode, bool ShowCloseButton, bool ShowMinButton, bool ShowMaxButton) : IWindowCommand;

    private sealed record SetShowInTaskbar(bool ShowInTaskbar) : IWindowCommand;

    private sealed record SetIsClickThrough(bool IsClickThrough) : IWindowCommand;

    private sealed record SetOpacity(double Opacity) : IWindowCommand;

    private sealed record SetBackgroundColor(Color BackgroundColor) : IWindowCommand;

    private sealed record SetBorderColor(Color BorderColor) : IWindowCommand;

    private sealed record SetBorderThickness(Thickness BorderThickness) : IWindowCommand;
    
    private sealed record SetBlazorUnityContainer(IUnityContainer ChildContainer) : IWindowCommand;
    
    private sealed record SetBlazorFileProvider(IFileProvider FileProvider) : IWindowCommand;
    
    private sealed record SetBlazorAdditionalFiles(ImmutableArray<IFileInfo> AdditionalFiles) : IWindowCommand;

    private sealed record IsVisibleChangedEvent(bool IsVisible) : IWindowEvent;

    private sealed record WindowTitleChangedEvent(string Title) : IWindowEvent;

    private sealed record WindowPosChangedEvent(Point Location) : IWindowEvent;

    private sealed record WindowSizeChangedEvent(Size Size) : IWindowEvent;

    private interface IWindowCommand : IWindowEvent
    {
    }

    private interface IWindowEvent
    {
    }

    private sealed record PropertyValueHolder<TValue> : DisposableReactiveRecord
    {
        private readonly BehaviorSubject<PropertyState<TValue>> stateSubject;

        public PropertyValueHolder(
            BlazorWindow owner,
            string propertyToRaise)
        {
            PropertyToRaise = propertyToRaise;
            stateSubject = new BehaviorSubject<PropertyState<TValue>>(default)
                .AddTo(Anchors);

            stateSubject
                .Subscribe(x => State = x)
                .AddTo(Anchors);

            stateSubject
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.Internal)
                .Subscribe(x => owner.RaisePropertyChanged(PropertyToRaise))
                .AddTo(Anchors);
        }

        public PropertyState<TValue> State { get; private set; }

        public string PropertyToRaise { get; }

        public IObservable<PropertyState<TValue>> Listen()
        {
            return Anchors.IsDisposed ? Observable.Return(State) : stateSubject;
        }

        public PropertyState<TValue> SetValue(TValue value, TrackedPropertyUpdateSource updateSource)
        {
            if (Anchors.IsDisposed)
            {
                return State;
            }
            
            var currentState = State;
            if (currentState.UpdateSource == updateSource && EqualityComparer<TValue>.Default.Equals(value, currentState.Value))
            {
                return currentState;
            }

            var newState = new PropertyState<TValue>()
            {
                Value = value,
                UpdateSource = updateSource,
                Revision = currentState.Revision + 1
            };
            stateSubject.OnNext(newState);
            return newState;
        }
    }

    private readonly record struct PropertyState<TValue>
    {
        public required TValue Value { get; init; }
        public required TrackedPropertyUpdateSource UpdateSource { get; init; }
        public required long Revision { get; init; }
    }

    private sealed class NativeWindow : ReactiveMetroWindowBase
    {
        private readonly BlazorWindow owner;

        public NativeWindow(BlazorWindow owner)
        {
            this.owner = owner;

            owner
                .WhenAnyValue(x => x.Container)
                .Select(x => x ?? owner.unityContainer)
                .Subscribe(x => ParentContainer = x)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.ParentContainer)
                .Subscribe(parentContainer =>
                {
                    var childContainer = parentContainer.CreateChildContainer().AddTo(Anchors);
                    childContainer.RegisterSingleton<IBlazorWindowController>(_ => this);
                    ChildContainer = childContainer;
                })
                .AddTo(Anchors);
            
            ContentControl = new BlazorContentControl()
            {
                Container = ChildContainer,
                ViewType = typeof(BlazorWindowContent),
                Content = owner
            }.AddTo(Anchors);
            Content = ContentControl;
            AllowsTransparency = true;
            
            Anchors.Add(() => Log.Debug("Disposed native window"));
        }

        public BlazorContentControl ContentControl { get; }

        public IUnityContainer ChildContainer { get; private set; }
        
        public IUnityContainer ParentContainer { get; private set; }
    }
}