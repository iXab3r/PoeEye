using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Extensions.FileProviders;
using PInvoke;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf.Automation;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Unity;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Things to note:
/// - if Application is Shutting down, windows WILL NOT be created, this is in Window code. Need to track it.
/// </summary>
internal partial class BlazorWindow : DisposableReactiveObjectWithLogger, IWpfBlazorWindow, IBlazorWindowMetroController, IBlazorWindowAutomationIdentity
{
    private static readonly Color DefaultBackgroundColor = Color.FromArgb(0xFF, 0x42, 0x42, 0x42);

    private readonly IUnityContainer unityContainer;
    private readonly Lazy<NativeWindow> windowSupplier;
    private bool allowsTransparency = true;
    private TitleBarDisplayMode titleBarDisplayMode = TitleBarDisplayMode.Default;

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
    private readonly PropertyValueHolder<bool> showActivated;
    private readonly PropertyValueHolder<WindowState> windowState;
    private readonly PropertyValueHolder<bool> noActivate;

    private readonly BlockingCollection<IWindowEvent> eventQueue;
    private readonly Dispatcher uiDispatcher;
    private readonly TaskCompletionSource isClosedTcs;
    private readonly SerialDisposable dragAnchor;
    private readonly ReactiveCompositeFileProvider compositeFileProvider;
    private readonly SerialDisposable additionalFileProviderAnchor;
    private readonly SerialDisposable windowSubscriptionAnchor;
    private IntPtr dialogOwnerHandle;

    // Logging throttling: limit certain verbose logs 
    private readonly Stopwatch logThrottleStopwatch = Stopwatch.StartNew();
    private long lastSetPosLogMs;
    private long lastSetSizeLogMs;
    private long lastSetRectLogMs;
    public BlazorWindow(
        IUnityContainer unityContainer,
        [OptionalDependency] IBlazorWindowConfigurator windowConfigurator = null,
        [OptionalDependency] Dispatcher dispatcher = null)
    {
        Log.AddSuffix($"BWnd#{windowId}");
        Log.Debug("New window is being created");
        this.unityContainer = unityContainer;
        isClosedTcs = new TaskCompletionSource();
        this.uiDispatcher = dispatcher ?? BlazorDispatcherProvider.Instance.GetOrAdd("BlazorWindow").Dispatcher;
        windowSupplier = new Lazy<NativeWindow>(() => CreateWindow());
        eventQueue = new BlockingCollection<IWindowEvent>();
        dragAnchor = new SerialDisposable().AddTo(Anchors);
        compositeFileProvider = new ReactiveCompositeFileProvider().AddTo(Anchors);
        additionalFileProviderAnchor = new SerialDisposable().AddTo(Anchors);
        windowSubscriptionAnchor = new SerialDisposable().AddTo(Anchors);

#pragma warning disable CS0618 // Type or member is obsolete
        this.RaiseWhenSourceValue(x => x.ViewDataContext, this, x => x.DataContext).AddTo(Anchors);
#pragma warning restore CS0618 // Type or member is obsolete
        
        Disposable.Create(() =>
        {
            try
            {
                if (!isClosedTcs.TrySetResult())
                {
                    Log.Debug($"Could not notify about window disposal, tcs: {isClosedTcs}");
                }

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
        showActivated = new PropertyValueHolder<bool>(this, nameof(ShowActivated)).AddTo(Anchors);
        noActivate = new PropertyValueHolder<bool>(this, nameof(NoActivate)).AddTo(Anchors);
        windowState = new PropertyValueHolder<WindowState>(this, nameof(WindowState)).AddTo(Anchors);

        ShowActivated = false;
        ShowInTaskbar = true;
        Width = 300;
        Height = 200;
        ShowMinButton = true;
        ShowMaxButton = true;
        ShowCloseButton = true;

        var whenLoadedSource = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => Loaded += h, h => Loaded -= h)
            .Select(x => x.EventArgs)
            .Take(1)
            .Replay(1);
        whenLoadedSource.Connect().AddTo(Anchors);
        WhenLoaded = whenLoadedSource;

        var whenUnloadedSource = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => Unloaded += h, h => Unloaded -= h)
            .Select(x => x.EventArgs)
            .Take(1)
            .Replay(1);
        whenUnloadedSource.Connect().AddTo(Anchors);
        WhenUnloaded = whenUnloadedSource;

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

        WhenMouseDown =
            Observable
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => MouseDown += h, h => MouseDown -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenMouseUp =
            Observable
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => MouseUp += h, h => MouseUp -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenPreviewMouseDown =
            Observable
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => PreviewMouseDown += h, h => PreviewMouseDown -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenPreviewMouseUp =
            Observable
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => PreviewMouseUp += h, h => PreviewMouseUp -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenPreviewMouseMove =
            Observable
                .FromEventPattern<MouseEventHandler, MouseEventArgs>(h => PreviewMouseMove += h, h => PreviewMouseMove -= h)
                .Select(x => x.EventArgs)
                .Publish()
                .RefCount();

        WhenMouseMove =
            Observable
                .FromEventPattern<MouseEventHandler, MouseEventArgs>(h => MouseMove += h, h => MouseMove -= h)
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
        
        Disposable.Create(() =>
        {
            Log.Debug("Blazor window is disposed, clenaing up references");
            ViewType = null;
            ViewTypeForTitleBar = null;
            Container = null;
            DataContext = null;
            ControlConfigurator = null;
        }).AddTo(Anchors);

        if (windowConfigurator != null)
        {
            var sw = ValueStopwatch.StartNew();
            var configuratorType = windowConfigurator.GetType();
            Log.Debug($"Invoking {nameof(IBlazorWindowConfigurator)} {configuratorType.FullName}");
            try
            {
                windowConfigurator.Configure(this);
            }
            finally
            {
                Log.Debug($"{nameof(IBlazorWindowConfigurator)} {configuratorType.FullName} completed in {sw.ElapsedMilliseconds}ms");
            }
        }
    }

    public Type ViewType { get; set; }

    public Type ViewTypeForTitleBar { get; set; }

    public object DataContext { get; set; }

    [Obsolete($"Replaced with {nameof(DataContext)} - to be removed in future versions")]
    [Browsable(false)]
    public object ViewDataContext
    {
        get => DataContext;
        set => DataContext = value;
    }

    public IUnityContainer Container { get; set; }

    public string AutomationId { get; set; }

    public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterOwner;

    public IntPtr OwnerHandle { get; set; }

    public new IFluentLog Log => base.Log;

    public ResizeMode ResizeMode { get; set; } = ResizeMode.CanResizeWithGrip;

    public TitleBarDisplayMode TitleBarDisplayMode
    {
        get => titleBarDisplayMode;
        set
        {
            if (titleBarDisplayMode == value)
            {
                return;
            }

            if (!TryPrepareTitleBarDisplayMode(value))
            {
                return;
            }

            RaiseAndSetIfChanged(ref titleBarDisplayMode, value);
        }
    }

    public bool AllowsTransparency
    {
        get => allowsTransparency;
        set
        {
            if (allowsTransparency == value)
            {
                return;
            }

            if (TitleBarDisplayMode.ResolveForWpf() == TitleBarDisplayMode.System && value)
            {
                Log.Warn($"{nameof(AllowsTransparency)} cannot be enabled while {nameof(TitleBarDisplayMode)} is {TitleBarDisplayMode.System}");
                return;
            }

            if (windowSupplier.IsValueCreated && windowSupplier.Value.WindowHandle != IntPtr.Zero)
            {
                Log.Warn($"Ignoring change to {nameof(AllowsTransparency)} after the native window handle is created");
                return;
            }

            RaiseAndSetIfChanged(ref allowsTransparency, value);
        }
    }

    private bool TryPrepareTitleBarDisplayMode(TitleBarDisplayMode value)
    {
        if (value.ResolveForWpf() != TitleBarDisplayMode.System || !allowsTransparency)
        {
            return true;
        }

        if (windowSupplier.IsValueCreated && windowSupplier.Value.WindowHandle != IntPtr.Zero)
        {
            Log.Warn($"Ignoring change to {nameof(TitleBarDisplayMode)} after the native window handle is created because {nameof(AllowsTransparency)} is enabled");
            return false;
        }

        Log.Debug($"Disabling {nameof(AllowsTransparency)} because effective {nameof(TitleBarDisplayMode)} is {TitleBarDisplayMode.System}");
        RaiseAndSetIfChanged(ref allowsTransparency, false);
        return true;
    }

    public IBlazorContentControlConfigurator ControlConfigurator { get; set; }

    public IDisposable RegisterFileProvider(IFileProvider fileProvider)
    {
        return compositeFileProvider.Add(fileProvider);
    }

    public bool ShowInTaskbar
    {
        get => showInTaskbar.State.Value;
        set => showInTaskbar.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public bool ShowActivated
    {
        get => showActivated.State.Value;
        set => showActivated.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public WindowState WindowState
    {
        get => windowState.State.Value;
        set => windowState.SetValue(value, TrackedPropertyUpdateSource.External);
    }

    public bool NoActivate
    {
        get => noActivate.State.Value;
        set => noActivate.SetValue(value, TrackedPropertyUpdateSource.External);
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
    public IObservable<MouseButtonEventArgs> WhenMouseDown { get; }
    public IObservable<MouseButtonEventArgs> WhenMouseUp { get; }
    public IObservable<MouseEventArgs> WhenMouseMove { get; }
    public IObservable<MouseButtonEventArgs> WhenPreviewMouseDown { get; }
    public IObservable<MouseButtonEventArgs> WhenPreviewMouseUp { get; }
    public IObservable<MouseEventArgs> WhenPreviewMouseMove { get; }
    public IObservable<EventArgs> WhenLoaded { get; }
    public IObservable<EventArgs> WhenUnloaded { get; }
    public IObservable<EventArgs> WhenClosed { get; }
    public IObservable<CancelEventArgs> WhenClosing { get; }
    public IObservable<EventArgs> WhenActivated { get; }
    public IObservable<EventArgs> WhenDeactivated { get; }

    public event MouseButtonEventHandler MouseDown;
    public event MouseButtonEventHandler MouseUp;
    public event MouseButtonEventHandler PreviewMouseDown;
    public event MouseButtonEventHandler PreviewMouseUp;
    public event MouseEventHandler MouseMove;
    public event MouseEventHandler PreviewMouseMove;
    public event KeyEventHandler KeyDown;
    public event KeyEventHandler KeyUp;
    public event KeyEventHandler PreviewKeyDown;
    public event KeyEventHandler PreviewKeyUp;
    public event CancelEventHandler Closing;
    public event EventHandler Activated;
    public event EventHandler Deactivated;
    public event EventHandler Loaded;
    public event EventHandler Unloaded;
    public event EventHandler Closed;

    public void ShowDevTools()
    {
        Log.Debug("Enqueueing ShowDevTools command");
        EnqueueUpdate(new ShowDevToolsCommand());
    }

    public void BeginInvoke(Action action)
    {
        using var resetEvent = new ManualResetEventSlim();
        EnqueueUpdate(new InvokeCommand(action, resetEvent, DateTimeOffset.Now));
    }

    public void WaitForIdle(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        using var resetEvent = new ManualResetEventSlim();
        EnqueueUpdate(new WaitForIdleCommand(resetEvent, DateTimeOffset.Now));

        Log.Debug($"Enqueueing WaitForIdle command");
        if (timeout > TimeSpan.Zero)
        {
            Log.Debug($"Awaiting for window being idle, timeout: {timeout}");
            resetEvent.Wait(timeout);
        }
        else
        {
            Log.Debug($"Awaiting for window being idle without timeout");
            resetEvent.Wait();
        }

        var elapsed = sw.Elapsed;
        Log.Debug($"All events in the queue have been processed, elapsed: {elapsed.TotalMilliseconds:F0}ms ({elapsed.Ticks:F0} ticks)");
    }

    public void Minimize()
    {
        Log.Debug("Enqueueing Minimize command");
        EnqueueUpdate(new MinimizeCommand());
    }

    public void Maximize()
    {
        Log.Debug("Enqueueing Maximize command");
        EnqueueUpdate(new MaximizeCommand());
    }

    public void Restore()
    {
        Log.Debug("Enqueueing Restore command");
        EnqueueUpdate(new RestoreCommand());
    }

    public void Hide()
    {
        Log.Debug("Enqueueing Hide command");
        EnqueueUpdate(new SetVisibleCommand(false));
    }

    public void Show()
    {
        EnsureNotDisposed();
        EnqueueUpdate(new SetVisibleCommand(true));

        Log.Debug("Showing window in non-blocking way");
    }

    public void Activate()
    {
        Log.Debug("Enqueueing Activate command");
        EnqueueUpdate(new ActivateCommand());
    }

    public void ShowDialog(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        Log.Debug($"Showing window in modal blocking way, current state: {{IsVisible={IsVisible}, StartupLocation={WindowStartupLocation}, Title={Title}}}");
        var completionSource = new TaskCompletionSource<bool>();
        EnqueueUpdate(new ShowDialogCommand(cancellationToken, completionSource));
        try
        {
            Log.Debug("Awaiting for the window to be closed");
            using var cancellationAnchor = cancellationToken.Register(() =>
            {
                Log.Debug("Cancellation requested while awaiting modal dialog completion");
                completionSource.TrySetCanceled(cancellationToken);
            });
            completionSource.Task.GetAwaiter().GetResult();
            Log.Debug("Modal dialog completed successfully");
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Wait was cancelled");
        }
        catch (Exception e)
        {
            Log.Warn("Modal dialog failed while waiting for completion", e);
            throw;
        }
    }

    public void Close()
    {
        EnsureNotDisposed();
        Log.Debug("Closing the window");
        EnqueueUpdate(new CloseCommand());
    }

    public IDisposable EnableDragMove()
    {
        EnsureNotDisposed();
        Log.Debug("Starts dragging the window");
        // Keep drag start serialized with the rest of the window command queue, but hand the actual move loop to the OS.
        var anchor = new CompositeDisposable();
        EnqueueUpdate(new StartDragCommand(anchor));
        return anchor;
    }

    public IDisposable EnableResize(WindowResizeDirection direction)
    {
        EnsureNotDisposed();
        Log.Debug($"Starts resizing the window from {direction}");
        // Keep resize start serialized with the rest of the window command queue, but hand the actual resize loop to the OS.
        var anchor = new CompositeDisposable();
        EnqueueUpdate(new StartResizeCommand(direction, anchor));
        return anchor;
    }

    public Dispatcher Dispatcher => uiDispatcher;

    public IntPtr GetWindowHandle()
    {
        var window = GetWindowOrThrow();
        return window.WindowHandle;
    }

    public Window GetWindow()
    {
        return GetOrCreate();
    }

    private void StartNativeDragMoveCore()
    {
        var hwnd = GetWindowHandle();
        var cursorPosition = UnsafeNative.GetCursorPosition();
        var cursorWindow = UnsafeNative.GetWindowUnderCursor();
        // Hand off to the OS non-client move loop so dragging stays smooth even if the Blazor/WPF dispatcher is busy.
        Log.Debug($"Starting native move loop. Cursor={cursorPosition}, WindowRect={UnsafeNative.GetWindowRect(hwnd)}, UnderCursor={DescribeWindowHandle(cursorWindow)}");
        User32.ReleaseCapture();
        User32.SendMessage(
            hwnd,
            User32.WindowMessage.WM_NCLBUTTONDOWN,
            (IntPtr) NativeWindowHitTest.Caption,
            UnsafeNative.MakeLParam(cursorPosition.X, cursorPosition.Y));
    }

    private void StartNativeResizeCore(WindowResizeDirection direction)
    {
        if (direction == WindowResizeDirection.None)
        {
            Log.Debug("Ignoring resize request because direction is None");
            return;
        }

        var hwnd = GetWindowHandle();
        var cursorPosition = UnsafeNative.GetCursorPosition();
        var hitTest = ToNativeHitTest(direction);
        // Use the OS non-client resize loop for the same reason as dragging: it keeps mouse capture/release semantics reliable under load.
        Log.Debug($"Starting native resize loop from {direction}. Cursor={cursorPosition}, WindowRect={UnsafeNative.GetWindowRect(hwnd)}");
        User32.ReleaseCapture();
        User32.SendMessage(
            hwnd,
            User32.WindowMessage.WM_NCLBUTTONDOWN,
            (IntPtr) hitTest,
            UnsafeNative.MakeLParam(cursorPosition.X, cursorPosition.Y));
    }

    private static NativeWindowHitTest ToNativeHitTest(WindowResizeDirection direction)
    {
        return direction switch
        {
            WindowResizeDirection.Left => NativeWindowHitTest.Left,
            WindowResizeDirection.Top => NativeWindowHitTest.Top,
            WindowResizeDirection.Right => NativeWindowHitTest.Right,
            WindowResizeDirection.Bottom => NativeWindowHitTest.Bottom,
            WindowResizeDirection.TopLeft => NativeWindowHitTest.TopLeft,
            WindowResizeDirection.TopRight => NativeWindowHitTest.TopRight,
            WindowResizeDirection.BottomLeft => NativeWindowHitTest.BottomLeft,
            WindowResizeDirection.BottomRight => NativeWindowHitTest.BottomRight,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported resize direction")
        };
    }

    private enum NativeWindowHitTest
    {
        Caption = 2,
        Left = 10,
        Right = 11,
        Top = 12,
        TopLeft = 13,
        TopRight = 14,
        Bottom = 15,
        BottomLeft = 16,
        BottomRight = 17
    }

    private static string DescribeWindowHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return "<none>";
        }

        return $"{handle}, Title='{UnsafeNative.GetWindowTitle(handle)}', Class='{UnsafeNative.GetWindowClass(handle)}'";
    }

    public Rectangle GetWindowRect()
    {
        return new Rectangle(Left, Top, Width, Height);
    }

    public void SetWindowRect(Rectangle rect)
    {
        if (ShouldLogThrottled(ref lastSetRectLogMs))
        {
            Log.Debug($"Setting window rect to {rect} from {new Rectangle(Left, Top, Width, Height)}");
        }
        windowLeft.SetValue(rect.Left, TrackedPropertyUpdateSource.Internal);
        windowTop.SetValue(rect.Top, TrackedPropertyUpdateSource.Internal);
        windowWidth.SetValue(rect.Width, TrackedPropertyUpdateSource.Internal);
        windowHeight.SetValue(rect.Height, TrackedPropertyUpdateSource.Internal);
        EnqueueUpdate(new SetWindowRectCommand(rect));
    }

    public void SetWindowSize(Size windowSize)
    {
        if (ShouldLogThrottled(ref lastSetSizeLogMs))
        {
            Log.Debug($"Resizing window to {windowSize} from {new Size(Width, Height)}");
        }
        windowWidth.SetValue(windowSize.Width, TrackedPropertyUpdateSource.Internal);
        windowHeight.SetValue(windowSize.Height, TrackedPropertyUpdateSource.Internal);
        EnqueueUpdate(new SetWindowSizeCommand(windowSize));
    }

    public void SetWindowPos(Point windowPos)
    {
        if (ShouldLogThrottled(ref lastSetPosLogMs))
        {
            Log.Debug($"Moving window to {windowPos} from {new Point(Left, Top)}");
        }
        windowLeft.SetValue(windowPos.X, TrackedPropertyUpdateSource.Internal);
        windowTop.SetValue(windowPos.Y, TrackedPropertyUpdateSource.Internal);
        EnqueueUpdate(new SetWindowPosCommand(windowPos));
    }

    private ReactiveWindow GetWindowOrThrow()
    {
        //important! We do not check any states here, window could be even Disposed at this point
        if (!windowSupplier.IsValueCreated)
        {
            throw new InvalidOperationException("Window is not created yet");
        }

        return windowSupplier.Value;
    }

    private void EnqueueUpdate(IWindowEvent windowEvent)
    {
        eventQueue.Add(windowEvent);
        EnqueueUpdate().AndForget();
    }

    private async Task EnqueueUpdate()
    {
        if (uiDispatcher.CheckAccess())
        {
            HandleUpdate();
        }
        else
        {
            await uiDispatcher.InvokeAsync(HandleUpdate);
        }
    }

    private NativeWindow GetOrCreate()
    {
        NativeWindow window;
        if (!windowSupplier.IsValueCreated)
        {
            window = windowSupplier.Value;
            Log.Debug($"Subscribing to window {window}, events in queue: {eventQueue.Count}");
            windowSubscriptionAnchor.Disposable = SubscribeToWindow(Log, window, this)
                .SubscribeSafe(x =>
                {
                    EnqueueUpdate(x);
                }, Log.HandleUiException);
            Log.Debug("NativeWindow created and subscribed successfully");
        }
        else
        {
            window = windowSupplier.Value;
        }

        return window;
    }
    
    private bool ShouldLogThrottled(ref long lastLogMs, long minIntervalMs = 1000)
    {
        var now = logThrottleStopwatch.ElapsedMilliseconds;
        var last = Interlocked.Read(ref lastLogMs);
        if (now - last >= minIntervalMs)
        {
            Interlocked.Exchange(ref lastLogMs, now);
            return true;
        }

        return false;
    }

    private void HandleUpdate()
    {
        uiDispatcher.VerifyAccess();

        if (eventQueue.Count <= 0)
        {
            return;
        }
        
        try
        {
            while (eventQueue.TryTake(out var windowEvent))
            {
                try
                {
                    HandleEvent(windowEvent);
                }
                catch (Exception e)
                {
                    throw new InvalidStateException($"Failed to process event: {windowEvent}", e);
                }
            }
        }
        catch (Exception e)
        {
            if (Anchors.IsDisposed && e is ObjectDisposedException || e.InnerException is ObjectDisposedException)
            {
                Log.Warn("Disposal exception occurred after window has already been disposed");
            }
            Log.Error("Critical error in Window message queue", e);
            if (!isClosedTcs.TrySetException(e))
            {
                Log.Warn($"Failed to notify about exception, tcs: {isClosedTcs}");
            }

            throw;
        }
    }

    private NativeWindow CreateWindow()
    {
        uiDispatcher.VerifyAccess();

        var window = new NativeWindow(this)
        {
            WindowStartupLocation = WindowStartupLocation,
        };

        //do not add window to Anchors! It must be disposed by DisposeWindow command
        return window;
    }

    private void ShowDialogCore(NativeWindow window, CancellationToken cancellationToken)
    {
        uiDispatcher.VerifyAccess();

        if (window.IsVisible)
        {
            Log.Warn("Cannot show modal dialog because the target window is already visible");
            throw new InvalidOperationException("Cannot show a visible window as a dialog.");
        }

        dialogOwnerHandle = ResolveDialogOwnerHandle(window);
        var ownerWasAlreadyDisabled = false;
        try
        {
            if (dialogOwnerHandle != IntPtr.Zero)
            {
                Log.Debug($"Assigning dialog owner handle: {dialogOwnerHandle.ToHexadecimal()}");
                var windowInteropHelper = new WindowInteropHelper(window);
                windowInteropHelper.Owner = dialogOwnerHandle;
                ownerWasAlreadyDisabled = UnsafeNative.EnableWindow(dialogOwnerHandle, false);
                Log.Debug($"Disabled modal owner window: {dialogOwnerHandle.ToHexadecimal()}, previously disabled: {ownerWasAlreadyDisabled}");
            }
            else
            {
                Log.Debug("Showing modal dialog without owner handle");
            }

            using var cancellationAnchor = cancellationToken.Register(() =>
            {
                try
                {
                    Log.Debug("Cancellation requested while modal dialog is open, closing modal window");
                    window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (!window.IsDisposed)
                        {
                            Log.Debug("Closing modal window because cancellation token was cancelled");
                            window.Close();
                        }
                        else
                        {
                            Log.Debug("Modal window was already disposed when cancellation was processed");
                        }
                    }));
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to close modal window on cancellation", e);
                }
            });

            Log.Debug("Entering native WPF ShowDialog()");
            window.ShowDialog();
            Log.Debug("Native WPF ShowDialog() has returned");
        }
        catch (Exception e)
        {
            Log.Warn("Failed to show modal window", e);
            throw;
        }
        finally
        {
            if (dialogOwnerHandle != IntPtr.Zero)
            {
                if (ownerWasAlreadyDisabled)
                {
                    Log.Debug("Skipping owner re-enable because owner window was already disabled before ShowDialog()");
                }
                else
                {
                    UnsafeNative.EnableWindow(dialogOwnerHandle, true);
                    Log.Debug($"Re-enabled modal owner window: {dialogOwnerHandle.ToHexadecimal()}");
                }
            }

            Log.Debug("Clearing modal dialog owner handle state");
            dialogOwnerHandle = IntPtr.Zero;
        }
    }

    private IntPtr ResolveDialogOwnerHandle(NativeWindow window)
    {
        var ownerHandle = OwnerHandle;
        if (ownerHandle == IntPtr.Zero)
        {
            Log.Debug("Modal dialog owner handle is not configured");
            return IntPtr.Zero;
        }

        if (!PInvoke.User32.IsWindow(ownerHandle))
        {
            Log.Warn($"Configured owner handle is invalid, ignoring owner: {ownerHandle.ToHexadecimal()}");
            return IntPtr.Zero;
        }

        try
        {
            if (window.WindowHandle != IntPtr.Zero && ownerHandle == window.WindowHandle)
            {
                Log.Debug("Configured owner handle points to the dialog window itself, ignoring owner");
                return IntPtr.Zero;
            }
        }
        catch
        {
            // ignored, handle may not exist before first show
        }

        Log.Debug($"Using configured owner handle for modal dialog: {ownerHandle.ToHexadecimal()}");
        return ownerHandle;
    }

    private void ActivateWindowWhenReady(NativeWindow window, string reason)
    {
        uiDispatcher.VerifyAccess();

        if (window == null || window.IsDisposed)
        {
            return;
        }

        void ActivateCore()
        {
            if (window.IsDisposed || !window.IsVisible || NoActivate)
            {
                return;
            }

            try
            {
                UnsafeNative.SetForegroundWindow(window.WindowHandle);
            }
            catch (InvalidOperationException e)
            {
                Log.Warn($"Failed to activate window during {reason}", e);
            }
        }

        if (window.IsLoaded)
        {
            window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ActivateCore));
            return;
        }

        window.WhenLoaded()
            .Take(1)
            .Subscribe(_ => window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ActivateCore)))
            .AddTo(window.Anchors);
    }
}
