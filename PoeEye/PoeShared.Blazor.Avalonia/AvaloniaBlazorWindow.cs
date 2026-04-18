using System.Collections.Immutable;
using System.ComponentModel;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PInvoke;
using PropertyChanged;
using PoeShared.Native;
using PoeShared.Blazor.Wpf;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using System.Windows;
using WpfDispatcher = System.Windows.Threading.Dispatcher;
using Unity;
using AvaloniaWindow = Avalonia.Controls.Window;
using AvaloniaWindowState = Avalonia.Controls.WindowState;
using AvaloniaWindowStartupLocation = Avalonia.Controls.WindowStartupLocation;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKeyEventHandler = System.Windows.Input.KeyEventHandler;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;
using WpfResizeMode = System.Windows.ResizeMode;
using WpfThickness = System.Windows.Thickness;
using WpfWindowState = System.Windows.WindowState;
using WpfWindowStartupLocation = System.Windows.WindowStartupLocation;

namespace PoeShared.Blazor.Avalonia;

public sealed class AvaloniaBlazorWindow : DisposableReactiveObjectWithLogger, IBlazorWindow
{
    private readonly int browserDebugPort;
    private readonly AvaloniaWindow? ownerWindow;
    private readonly Subject<EventArgs> whenLoaded = new();
    private readonly Subject<EventArgs> whenClosed = new();
    private readonly Subject<CancelEventArgs> whenClosing = new();
    private readonly Subject<EventArgs> whenActivated = new();
    private readonly Subject<EventArgs> whenDeactivated = new();
    private readonly List<IFileProvider> registeredFileProviders = new();
    private AvaloniaHostWindow? window;
    private AvaloniaBlazorContentHost? contentHost;
    private Type viewType = typeof(AvaloniaPlaceholderView);
    private Type? viewTypeForTitleBar;
    private object? dataContext;
    private object? viewDataContext;
    private IReadOnlyDictionary<string, object?> viewParameters = ImmutableDictionary<string, object?>.Empty;
    private Action<IServiceCollection>? configureServices;
    private IUnityContainer? container;
    private ImmutableArray<IFileInfo> additionalFiles = ImmutableArray<IFileInfo>.Empty;
    private IFileProvider? additionalFileProvider;
    private IBlazorContentControlConfigurator? controlConfigurator;
    private WpfResizeMode resizeMode = WpfResizeMode.CanResize;
    private TitleBarDisplayMode titleBarDisplayMode = TitleBarDisplayMode.Default;
    private bool allowsTransparency;
    private bool showInTaskbar = true;
    private bool showActivated = true;
    private WpfThickness padding = new(0);
    private bool isClickThrough;
    private bool isDebugMode;
    private bool isVisible;
    private double opacity = 1.0;
    private string title = "PoeShared Avalonia Window";
    private bool topmost;
    private bool noActivate;
    private bool showCloseButton = true;
    private bool showMinButton = true;
    private bool showMaxButton = true;
    private int left;
    private int top;
    private int width = 900;
    private int height = 600;
    private int minWidth = 300;
    private int minHeight = 200;
    private int maxWidth = int.MaxValue;
    private int maxHeight = int.MaxValue;
    private WpfColor backgroundColor = WpfColors.White;
    private WpfColor borderColor = WpfColors.Transparent;
    private WpfThickness borderThickness = new(0);
    private WpfWindowState windowState = WpfWindowState.Normal;
    private WpfWindowStartupLocation windowStartupLocation = WpfWindowStartupLocation.Manual;
    private IntPtr ownerHandle;
    private bool disposed;
    private bool lifecycleSignalsCompleted;
    private ManualResizeSession? manualResizeSession;
    private string lastBoundsApplyStatus = "idle";
    private string automationId = string.Empty;
    private const double ResizeHandleThickness = 4;

    public AvaloniaBlazorWindow(int browserDebugPort = 0, AvaloniaWindow? ownerWindow = null)
    {
        this.browserDebugPort = browserDebugPort;
        this.ownerWindow = ownerWindow ?? ResolveOwnerWindow();
    }

    public new IFluentLog Log => base.Log;

    public Type ViewType
    {
        get => viewType;
        set
        {
            value = value ?? throw new ArgumentNullException(nameof(value));
            if (viewType == value)
            {
                return;
            }

            viewType = value;
            RebuildContentHost();
            RaisePropertyChanged();
        }
    }

    public Type? ViewTypeForTitleBar
    {
        get => viewTypeForTitleBar;
        set => RaiseAndSetIfChanged(ref viewTypeForTitleBar, value);
    }

    public object DataContext
    {
        get => dataContext!;
        set
        {
            if (ReferenceEquals(dataContext, value))
            {
                return;
            }

            dataContext = value;
            if (window != null)
            {
                window.DataContext = value;
                RebuildContentHost();
            }

            RaisePropertyChanged();
        }
    }

    public object ViewDataContext
    {
        get => DataContext;
        set => DataContext = value;
    }

    public IReadOnlyDictionary<string, object?> ViewParameters
    {
        get => viewParameters;
        set
        {
            var normalized = value ?? ImmutableDictionary<string, object?>.Empty;
            if (ReferenceEquals(viewParameters, normalized))
            {
                return;
            }

            viewParameters = normalized;
            if (window != null)
            {
                RebuildContentHost();
            }

            RaisePropertyChanged();
        }
    }

    public Action<IServiceCollection>? ConfigureServices
    {
        get => configureServices;
        set
        {
            if (ReferenceEquals(configureServices, value))
            {
                return;
            }

            configureServices = value;
            if (window != null)
            {
                RebuildContentHost();
            }

            RaisePropertyChanged();
        }
    }

    public IUnityContainer Container
    {
        get => container!;
        set
        {
            RaiseAndSetIfChanged(ref container, value);
            if (window != null)
            {
                RebuildContentHost();
            }
        }
    }

    public ImmutableArray<IFileInfo> AdditionalFiles
    {
        get => additionalFiles;
        set => RaiseAndSetIfChanged(ref additionalFiles, value);
    }

    public IFileProvider AdditionalFileProvider
    {
        get => additionalFileProvider!;
        set => RaiseAndSetIfChanged(ref additionalFileProvider, value);
    }

    public IBlazorContentControlConfigurator ControlConfigurator
    {
        get => controlConfigurator!;
        set => RaiseAndSetIfChanged(ref controlConfigurator, value);
    }

    public WpfResizeMode ResizeMode
    {
        get => resizeMode;
        set
        {
            RaiseAndSetIfChanged(ref resizeMode, value);
            ApplyWindowChrome();
        }
    }

    public TitleBarDisplayMode TitleBarDisplayMode
    {
        get => titleBarDisplayMode;
        set
        {
            RaiseAndSetIfChanged(ref titleBarDisplayMode, value);
            ApplyWindowChrome();
        }
    }

    public bool AllowsTransparency
    {
        get => allowsTransparency;
        set => RaiseAndSetIfChanged(ref allowsTransparency, value);
    }

    public bool ShowInTaskbar
    {
        get => showInTaskbar;
        set
        {
            RaiseAndSetIfChanged(ref showInTaskbar, value);
            if (window != null)
            {
                window.ShowInTaskbar = value;
            }
        }
    }

    public bool ShowActivated
    {
        get => showActivated;
        set => RaiseAndSetIfChanged(ref showActivated, value);
    }

    public WpfThickness Padding
    {
        get => padding;
        set => RaiseAndSetIfChanged(ref padding, value);
    }

    public bool IsClickThrough
    {
        get => isClickThrough;
        set => RaiseAndSetIfChanged(ref isClickThrough, value);
    }

    public bool IsDebugMode
    {
        get => isDebugMode;
        set => RaiseAndSetIfChanged(ref isDebugMode, value);
    }

    public bool IsVisible
    {
        get => isVisible;
        set => RaiseAndSetIfChanged(ref isVisible, value);
    }

    public double Opacity
    {
        get => opacity;
        set
        {
            RaiseAndSetIfChanged(ref opacity, value);
            if (window != null)
            {
                window.Opacity = value;
            }
        }
    }

    public string Title
    {
        get => title;
        set
        {
            RaiseAndSetIfChanged(ref title, value);
            if (window != null)
            {
                window.Title = value;
            }
        }
    }

    public bool Topmost
    {
        get => topmost;
        set
        {
            RaiseAndSetIfChanged(ref topmost, value);
            if (window != null)
            {
                window.Topmost = value;
            }
        }
    }

    public bool NoActivate
    {
        get => noActivate;
        set => RaiseAndSetIfChanged(ref noActivate, value);
    }

    public bool ShowCloseButton
    {
        get => showCloseButton;
        set => RaiseAndSetIfChanged(ref showCloseButton, value);
    }

    public bool ShowMinButton
    {
        get => showMinButton;
        set => RaiseAndSetIfChanged(ref showMinButton, value);
    }

    public bool ShowMaxButton
    {
        get => showMaxButton;
        set => RaiseAndSetIfChanged(ref showMaxButton, value);
    }

    public int Left
    {
        get => left;
        set
        {
            RaiseAndSetIfChanged(ref left, value);
            ApplyWindowBounds();
        }
    }

    public int Top
    {
        get => top;
        set
        {
            RaiseAndSetIfChanged(ref top, value);
            ApplyWindowBounds();
        }
    }

    public int Width
    {
        get => width;
        set
        {
            RaiseAndSetIfChanged(ref width, value);
            ApplyWindowBounds();
        }
    }

    public int Height
    {
        get => height;
        set
        {
            RaiseAndSetIfChanged(ref height, value);
            ApplyWindowBounds();
        }
    }

    public int MinWidth
    {
        get => minWidth;
        set
        {
            RaiseAndSetIfChanged(ref minWidth, value);
            ApplyWindowBounds();
        }
    }

    public int MinHeight
    {
        get => minHeight;
        set
        {
            RaiseAndSetIfChanged(ref minHeight, value);
            ApplyWindowBounds();
        }
    }

    public int MaxWidth
    {
        get => maxWidth;
        set
        {
            RaiseAndSetIfChanged(ref maxWidth, value);
            ApplyWindowBounds();
        }
    }

    public int MaxHeight
    {
        get => maxHeight;
        set
        {
            RaiseAndSetIfChanged(ref maxHeight, value);
            ApplyWindowBounds();
        }
    }

    public WpfColor BackgroundColor
    {
        get => backgroundColor;
        set => RaiseAndSetIfChanged(ref backgroundColor, value);
    }

    public WpfColor BorderColor
    {
        get => borderColor;
        set => RaiseAndSetIfChanged(ref borderColor, value);
    }

    public WpfThickness BorderThickness
    {
        get => borderThickness;
        set => RaiseAndSetIfChanged(ref borderThickness, value);
    }

    public WpfWindowState WindowState
    {
        get => windowState;
        set
        {
            RaiseAndSetIfChanged(ref windowState, value);
            if (window != null)
            {
                window.WindowState = ToAvaloniaWindowState(value);
            }
        }
    }

    public WpfWindowStartupLocation WindowStartupLocation
    {
        get => windowStartupLocation;
        set
        {
            RaiseAndSetIfChanged(ref windowStartupLocation, value);
            if (window != null)
            {
                window.WindowStartupLocation = ToAvaloniaWindowStartupLocation(value);
            }
        }
    }

    public IntPtr OwnerHandle
    {
        get => ownerHandle;
        set => RaiseAndSetIfChanged(ref ownerHandle, value);
    }

    public AvaloniaWindow NativeWindow
    {
        get
        {
            EnsureWindow();
            return window!;
        }
    }

    public string LastBoundsApplyStatus => lastBoundsApplyStatus;

    public string AutomationId
    {
        get => automationId;
        set => RaiseAndSetIfChanged(ref automationId, value?.Trim() ?? string.Empty);
    }

    public WpfDispatcher Dispatcher => WpfDispatcher.CurrentDispatcher;

    public IObservable<WpfKeyEventArgs> WhenKeyDown => Observable.Empty<WpfKeyEventArgs>();

    public IObservable<WpfKeyEventArgs> WhenKeyUp => Observable.Empty<WpfKeyEventArgs>();

    public IObservable<WpfKeyEventArgs> WhenPreviewKeyDown => Observable.Empty<WpfKeyEventArgs>();

    public IObservable<WpfKeyEventArgs> WhenPreviewKeyUp => Observable.Empty<WpfKeyEventArgs>();

    public IObservable<WpfMouseButtonEventArgs> WhenMouseDown => Observable.Empty<WpfMouseButtonEventArgs>();

    public IObservable<WpfMouseButtonEventArgs> WhenMouseUp => Observable.Empty<WpfMouseButtonEventArgs>();

    public IObservable<WpfMouseButtonEventArgs> WhenPreviewMouseDown => Observable.Empty<WpfMouseButtonEventArgs>();

    public IObservable<WpfMouseButtonEventArgs> WhenPreviewMouseUp => Observable.Empty<WpfMouseButtonEventArgs>();

    public IObservable<WpfMouseEventArgs> WhenMouseMove => Observable.Empty<WpfMouseEventArgs>();

    public IObservable<WpfMouseEventArgs> WhenPreviewMouseMove => Observable.Empty<WpfMouseEventArgs>();

    public IObservable<EventArgs> WhenLoaded => whenLoaded;

    public IObservable<EventArgs> WhenUnloaded => Observable.Empty<EventArgs>();

    public IObservable<EventArgs> WhenClosed => whenClosed;

    public IObservable<CancelEventArgs> WhenClosing => whenClosing;

    public IObservable<EventArgs> WhenActivated => whenActivated;

    public IObservable<EventArgs> WhenDeactivated => whenDeactivated;

    public event WpfKeyEventHandler? KeyDown;

    public event WpfKeyEventHandler? KeyUp;

    public event WpfKeyEventHandler? PreviewKeyDown;

    public event WpfKeyEventHandler? PreviewKeyUp;

    public event WpfMouseButtonEventHandler? MouseDown;

    public event WpfMouseButtonEventHandler? MouseUp;

    public event WpfMouseEventHandler? MouseMove;

    public event WpfMouseButtonEventHandler? PreviewMouseDown;

    public event WpfMouseButtonEventHandler? PreviewMouseUp;

    public event WpfMouseEventHandler? PreviewMouseMove;

    public event CancelEventHandler? Closing;

    public event EventHandler? Activated;

    public event EventHandler? Deactivated;

    public event EventHandler? Loaded;

    public event EventHandler? Closed;

    public void BeginInvoke(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        global::Avalonia.Threading.Dispatcher.UIThread.Post(action);
    }

    public void WaitForIdle(TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = TimeSpan.FromSeconds(5);
        }

        using var idleSignal = new ManualResetEventSlim(false);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => idleSignal.Set());
        idleSignal.Wait(timeout);
    }

    public void Minimize()
    {
        EnsureWindow();
        window!.WindowState = AvaloniaWindowState.Minimized;
    }

    public void Maximize()
    {
        EnsureWindow();
        window!.WindowState = AvaloniaWindowState.Maximized;
    }

    public void Restore()
    {
        EnsureWindow();
        window!.WindowState = AvaloniaWindowState.Normal;
    }

    public void Hide()
    {
        EnsureWindow();
        window!.Hide();
        isVisible = false;
        RaisePropertyChanged(nameof(IsVisible));
    }

    public void Show()
    {
        EnsureWindow();
        window!.Show();
        isVisible = true;
        RaisePropertyChanged(nameof(IsVisible));
    }

    public void Activate()
    {
        EnsureWindow();
        window!.Activate();
    }

    public void ShowDialog(CancellationToken cancellationToken = default)
    {
        EnsureWindow();
        if (ownerWindow != null)
        {
            _ = window!.ShowDialog(ownerWindow);
            isVisible = true;
            RaisePropertyChanged(nameof(IsVisible));
            return;
        }

        Show();
    }

    public void Close()
    {
        if (window == null)
        {
            return;
        }

        window.Close();
    }

    public IDisposable EnableDragMove()
    {
        EnsureWindow();
        global::Avalonia.Threading.Dispatcher.UIThread.Post(StartNativeDragMoveCore);
        return Disposable.Empty;
    }

    public IDisposable EnableResize(WindowResizeDirection direction)
    {
        if (direction == WindowResizeDirection.None)
        {
            return Disposable.Empty;
        }

        BeginResize(direction, null);
        return Disposable.Empty;
    }

    public void BeginResize(WindowResizeDirection direction, DrawingPoint? startScreenPoint)
    {
        if (direction == WindowResizeDirection.None)
        {
            return;
        }

        EnsureWindow();
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (window == null)
            {
                return;
            }

            StartNativeResizeCore(direction, startScreenPoint);
        });
    }

    public void ShowDevTools()
    {
        try
        {
            contentHost?.ShowDevTools();
        }
        catch (Exception e)
        {
            Log.Warn("Failed to open DevTools", e);
        }
    }

    internal void ReloadContentHost()
    {
        RunOnUiThread(RebuildContentHost);
    }

    internal Task ZoomInAsync()
    {
        return contentHost?.ZoomInAsync() ?? Task.CompletedTask;
    }

    internal Task ZoomOutAsync()
    {
        return contentHost?.ZoomOutAsync() ?? Task.CompletedTask;
    }

    internal Task ResetZoomAsync()
    {
        return contentHost?.ResetZoomAsync() ?? Task.CompletedTask;
    }

    public void BeginManualResize(WindowResizeDirection direction, DrawingPoint startScreenPoint)
    {
        RunOnUiThread(() =>
        {
            if (direction == WindowResizeDirection.None)
            {
                manualResizeSession = null;
                return;
            }

            EnsureWindow();
            manualResizeSession = new ManualResizeSession(direction, startScreenPoint, GetWindowRect());
        });
    }

    public void UpdateManualResize(DrawingPoint currentScreenPoint)
    {
        RunOnUiThread(() =>
        {
            if (manualResizeSession == null)
            {
                return;
            }

            ApplyResize(manualResizeSession.Direction, manualResizeSession.StartRect, manualResizeSession.StartPoint, currentScreenPoint);
        });
    }

    public void EndManualResize()
    {
        RunOnUiThread(() => manualResizeSession = null);
    }

    private void StartNativeResizeCore(WindowResizeDirection direction, DrawingPoint? startScreenPoint = null)
    {
        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var cursorPosition = startScreenPoint ?? UnsafeNative.GetCursorPosition();
        SetCursorPos(cursorPosition.X, cursorPosition.Y);
        ReleaseCapture();
        SendMessage(
            hwnd,
            WmNcLButtonDown,
            (IntPtr)ToNativeHitTest(direction),
            UnsafeNative.MakeLParam(cursorPosition.X, cursorPosition.Y));
    }

    private void StartNativeDragMoveCore()
    {
        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var cursorPosition = UnsafeNative.GetCursorPosition();
        SetCursorPos(cursorPosition.X, cursorPosition.Y);
        ReleaseCapture();
        SendMessage(
            hwnd,
            WmNcLButtonDown,
            (IntPtr)HtCaption,
            UnsafeNative.MakeLParam(cursorPosition.X, cursorPosition.Y));
    }

    private static int ToNativeHitTest(WindowResizeDirection direction)
    {
        return direction switch
        {
            WindowResizeDirection.Left => 10,
            WindowResizeDirection.Right => 11,
            WindowResizeDirection.Top => 12,
            WindowResizeDirection.TopLeft => 13,
            WindowResizeDirection.TopRight => 14,
            WindowResizeDirection.Bottom => 15,
            WindowResizeDirection.BottomLeft => 16,
            WindowResizeDirection.BottomRight => 17,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported resize direction")
        };
    }

    private void ApplyResize(WindowResizeDirection direction, DrawingRectangle startRect, DrawingPoint startCursor, DrawingPoint currentCursor)
    {
        var edge = ToWindowEdge(direction);
        var deltaX = currentCursor.X - startCursor.X;
        var deltaY = currentCursor.Y - startCursor.Y;
        var nextLeft = startRect.Left;
        var nextTop = startRect.Top;
        var nextWidth = startRect.Width;
        var nextHeight = startRect.Height;
        var startRight = startRect.Right;
        var startBottom = startRect.Bottom;

        if (TouchesWest(edge))
        {
            nextLeft = startRect.Left + deltaX;
            nextWidth = startRight - nextLeft;
        }
        else if (TouchesEast(edge))
        {
            nextWidth = startRect.Width + deltaX;
        }

        if (TouchesNorth(edge))
        {
            nextTop = startRect.Top + deltaY;
            nextHeight = startBottom - nextTop;
        }
        else if (TouchesSouth(edge))
        {
            nextHeight = startRect.Height + deltaY;
        }

        nextWidth = Math.Clamp(nextWidth, minWidth, maxWidth);
        nextHeight = Math.Clamp(nextHeight, minHeight, maxHeight);

        if (TouchesWest(edge))
        {
            nextLeft = startRight - nextWidth;
        }

        if (TouchesNorth(edge))
        {
            nextTop = startBottom - nextHeight;
        }

        SetWindowRect(new DrawingRectangle(nextLeft, nextTop, nextWidth, nextHeight));
    }

    private static WindowEdge ToWindowEdge(WindowResizeDirection direction)
        => direction switch
        {
            WindowResizeDirection.Left => WindowEdge.West,
            WindowResizeDirection.Right => WindowEdge.East,
            WindowResizeDirection.Top => WindowEdge.North,
            WindowResizeDirection.TopLeft => WindowEdge.NorthWest,
            WindowResizeDirection.TopRight => WindowEdge.NorthEast,
            WindowResizeDirection.Bottom => WindowEdge.South,
            WindowResizeDirection.BottomLeft => WindowEdge.SouthWest,
            WindowResizeDirection.BottomRight => WindowEdge.SouthEast,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported resize direction")
        };

    private static bool TouchesWest(WindowEdge edge)
        => edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest;

    private static bool TouchesEast(WindowEdge edge)
        => edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast;

    private static bool TouchesNorth(WindowEdge edge)
        => edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast;

    private static bool TouchesSouth(WindowEdge edge)
        => edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast;

    public IDisposable RegisterFileProvider(IFileProvider fileProvider)
    {
        if (fileProvider == null)
        {
            throw new ArgumentNullException(nameof(fileProvider));
        }

        lock (registeredFileProviders)
        {
            registeredFileProviders.Add(fileProvider);
        }

        return Disposable.Create(() =>
        {
            lock (registeredFileProviders)
            {
                registeredFileProviders.Remove(fileProvider);
            }
        });
    }

    public IntPtr GetWindowHandle()
    {
        var platformHandle = window?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (platformHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var rootHandle = User32.GetAncestor(platformHandle, User32.GetAncestorFlags.GA_ROOT);
        return rootHandle != IntPtr.Zero ? rootHandle : platformHandle;
    }

    public DrawingRectangle GetWindowRect()
    {
        if (window == null)
        {
            return DrawingRectangle.Empty;
        }

        var hwnd = GetWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            return UnsafeNative.GetWindowRect(hwnd);
        }

        var position = window.Position;
        var frameSize = window.FrameSize ?? window.ClientSize;
        return new DrawingRectangle(position.X, position.Y, (int)Math.Round(frameSize.Width), (int)Math.Round(frameSize.Height));
    }

    public void SetWindowRect(DrawingRectangle windowRect)
    {
        left = windowRect.Left;
        top = windowRect.Top;
        width = windowRect.Width;
        height = windowRect.Height;
        ApplyWindowBounds();
    }

    public void SetWindowSize(DrawingSize windowSize)
    {
        width = windowSize.Width;
        height = windowSize.Height;
        ApplyWindowBounds();
    }

    public void SetWindowPos(DrawingPoint windowPos)
    {
        left = windowPos.X;
        top = windowPos.Y;
        ApplyWindowBounds();
    }

    public override void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (window != null && window.IsVisible)
        {
            window.Close();
        }

        contentHost?.Dispose();
        CompleteLifecycleSignals();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EnsureWindow()
    {
        if (window != null)
        {
            return;
        }

        window = new AvaloniaHostWindow
        {
            Title = title,
            Width = width,
            Height = height,
            MinWidth = minWidth,
            MinHeight = minHeight,
            MaxWidth = maxWidth == int.MaxValue ? double.PositiveInfinity : maxWidth,
            MaxHeight = maxHeight == int.MaxValue ? double.PositiveInfinity : maxHeight,
            Topmost = topmost,
            ShowInTaskbar = showInTaskbar,
            Opacity = opacity,
            WindowState = ToAvaloniaWindowState(windowState),
            WindowStartupLocation = ToAvaloniaWindowStartupLocation(windowStartupLocation),
            SizeToContent = global::Avalonia.Controls.SizeToContent.Manual,
            DataContext = dataContext
        };
        if (windowStartupLocation == WpfWindowStartupLocation.Manual)
        {
            window.Position = new PixelPoint(left, top);
        }

        ApplyWindowChrome();
        window.Content = BuildWindowContent();
        ApplyWindowBounds();
        window.Opened += OnWindowOpened;
        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        window.PositionChanged += OnWindowPositionChanged;
        window.Resized += OnWindowResized;
    }

    private void ApplyWindowBounds()
    {
        RunOnUiThread(ApplyWindowBoundsCore);
    }

    private void ApplyWindowBoundsCore()
    {
        if (window == null)
        {
            lastBoundsApplyStatus = "window-null";
            return;
        }

        var targetLeft = left;
        var targetTop = top;
        var targetWidth = width;
        var targetHeight = height;

        window.SizeToContent = global::Avalonia.Controls.SizeToContent.Manual;
        window.MinWidth = minWidth;
        window.MinHeight = minHeight;
        window.MaxWidth = maxWidth == int.MaxValue ? double.PositiveInfinity : maxWidth;
        window.MaxHeight = maxHeight == int.MaxValue ? double.PositiveInfinity : maxHeight;
        window.Topmost = topmost;
        window.ShowInTaskbar = showInTaskbar;
        window.Opacity = opacity;
        window.WindowState = ToAvaloniaWindowState(windowState);
        window.WindowStartupLocation = ToAvaloniaWindowStartupLocation(windowStartupLocation);
        if (window.WindowState == AvaloniaWindowState.Normal)
        {
            var currentFrameSize = window.FrameSize ?? window.ClientSize;
            var frameChromeWidth = Math.Max(0, currentFrameSize.Width - window.ClientSize.Width);
            var frameChromeHeight = Math.Max(0, currentFrameSize.Height - window.ClientSize.Height);
            var targetClientWidth = Math.Max(1, targetWidth - frameChromeWidth);
            var targetClientHeight = Math.Max(1, targetHeight - frameChromeHeight);

            window.Position = new PixelPoint(targetLeft, targetTop);
            window.SetFrameSize(new global::Avalonia.Size(targetWidth, targetHeight));
            window.Width = targetClientWidth;
            window.Height = targetClientHeight;
            lastBoundsApplyStatus = $"avalonia-size={targetClientWidth:F0}x{targetClientHeight:F0}";
        }

        var appliedFrameSize = window.FrameSize ?? window.ClientSize;
        lastBoundsApplyStatus = $"{lastBoundsApplyStatus};scale={window.RenderScaling:F2};frame={appliedFrameSize.Width:F0}x{appliedFrameSize.Height:F0};client={window.ClientSize.Width:F0}x{window.ClientSize.Height:F0};logical={window.Width:F0}x{window.Height:F0};sizeToContent={window.SizeToContent};canResize={window.CanResize};decorations={window.WindowDecorations};pos={window.Position.X},{window.Position.Y};state={window.WindowState};target={targetLeft},{targetTop},{targetWidth},{targetHeight}";
    }

    private void ApplyWindowChrome()
    {
        if (window == null)
        {
            return;
        }

        var canResize = resizeMode is WpfResizeMode.CanResize or WpfResizeMode.CanResizeWithGrip;
        var effectiveTitleBarDisplayMode = titleBarDisplayMode.ResolveForAvalonia();

        window.CanResize = canResize;
        window.CanMinimize = showMinButton;
        window.CanMaximize = showMaxButton && canResize;
        window.ExtendClientAreaToDecorationsHint = false;
        window.WindowDecorations = effectiveTitleBarDisplayMode == TitleBarDisplayMode.System
            ? WindowDecorations.Full
            : WindowDecorations.None;
        window.ShowInTaskbar = showInTaskbar;
    }

    private void RebuildContentHost()
    {
        if (window == null)
        {
            return;
        }

        contentHost?.Dispose();
        contentHost = BuildContentHost();
        window.Content = BuildWindowContent();
    }

    private AvaloniaBlazorContentHost BuildContentHost()
    {
        return new AvaloniaBlazorContentHost(
            browserDebugPort,
            typeof(AvaloniaBlazorWindowContent),
            new Dictionary<string, object?>
            {
                [nameof(AvaloniaBlazorWindowContent.DataContext)] = this
            },
            configureServices: services =>
            {
                services.AddSingleton<AvaloniaBlazorWindow>(this);
                services.AddSingleton<IBlazorWindow>(this);
                services.AddSingleton<IBlazorHostController>(_ => new AvaloniaBlazorHostController(this));
                if (container != null)
                {
                    services.AddSingleton(container);
                }

                configureServices?.Invoke(services);
            });
    }

    private global::Avalonia.Controls.Control BuildWindowContent()
    {
        contentHost ??= BuildContentHost();
        var canResize = resizeMode is WpfResizeMode.CanResize or WpfResizeMode.CanResizeWithGrip;
        var layout = new Grid
        {
            RowDefinitions = canResize
                ? new RowDefinitions($"{ResizeHandleThickness},*,{ResizeHandleThickness}")
                : new RowDefinitions("0,*,0"),
            ColumnDefinitions = canResize
                ? new ColumnDefinitions($"{ResizeHandleThickness},*,{ResizeHandleThickness}")
                : new ColumnDefinitions("0,*,0")
        };

        var contentBorder = new Border
        {
            Padding = new global::Avalonia.Thickness(0),
            Child = contentHost
        };
        Grid.SetRow(contentBorder, 1);
        Grid.SetColumn(contentBorder, 1);
        layout.Children.Add(contentBorder);

        if (!canResize)
        {
            return layout;
        }

        AddResizeHandle(layout, WindowEdge.North, 0, 1, StandardCursorType.TopSide);
        AddResizeHandle(layout, WindowEdge.South, 2, 1, StandardCursorType.BottomSide);
        AddResizeHandle(layout, WindowEdge.West, 1, 0, StandardCursorType.LeftSide);
        AddResizeHandle(layout, WindowEdge.East, 1, 2, StandardCursorType.RightSide);
        AddResizeHandle(layout, WindowEdge.NorthWest, 0, 0, StandardCursorType.TopLeftCorner);
        AddResizeHandle(layout, WindowEdge.NorthEast, 0, 2, StandardCursorType.TopRightCorner);
        AddResizeHandle(layout, WindowEdge.SouthWest, 2, 0, StandardCursorType.BottomLeftCorner);
        AddResizeHandle(layout, WindowEdge.SouthEast, 2, 2, StandardCursorType.BottomRightCorner);

        return layout;
    }

    private void AddResizeHandle(Grid layout, WindowEdge edge, int row, int column, StandardCursorType cursor)
    {
        var handle = new Border
        {
            Background = global::Avalonia.Media.Brushes.Transparent,
            Cursor = new global::Avalonia.Input.Cursor(cursor)
        };
        handle.PointerPressed += (_, e) =>
        {
            if (window == null)
            {
                return;
            }

            window.BeginResizeDrag(edge, e);
            e.Handled = true;
        };
        Grid.SetRow(handle, row);
        Grid.SetColumn(handle, column);
        layout.Children.Add(handle);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        ApplyWindowBoundsCore();
        SyncWindowMetricsFromWindow();
        isVisible = true;
        RaisePropertyChanged(nameof(IsVisible));
        whenLoaded.OnNext(e);
        Loaded?.Invoke(this, e);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        whenClosing.OnNext(e);
        Closing?.Invoke(this, e);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (window != null)
        {
            window.PositionChanged -= OnWindowPositionChanged;
            window.Resized -= OnWindowResized;
        }

        isVisible = false;
        RaisePropertyChanged(nameof(IsVisible));
        whenClosed.OnNext(e);
        Closed?.Invoke(this, e);
        CompleteLifecycleSignals();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        whenActivated.OnNext(e);
        Activated?.Invoke(this, e);
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        whenDeactivated.OnNext(e);
        Deactivated?.Invoke(this, e);
    }

    private void OnWindowPositionChanged(object? sender, PixelPointEventArgs e)
    {
        SyncWindowMetricsFromWindow();
    }

    private void OnWindowResized(object? sender, WindowResizedEventArgs e)
    {
        SyncWindowMetricsFromWindow();
    }

    private void SyncWindowMetricsFromWindow()
    {
        if (window == null)
        {
            return;
        }

        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(SyncWindowMetricsFromWindow, global::Avalonia.Threading.DispatcherPriority.Background);
            return;
        }

        var hwnd = GetWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            var nativeRect = UnsafeNative.GetWindowRect(hwnd);
            left = nativeRect.Left;
            top = nativeRect.Top;
            width = nativeRect.Width;
            height = nativeRect.Height;
        }
        else
        {
            var frameSize = window.FrameSize ?? window.ClientSize;
            left = window.Position.X;
            top = window.Position.Y;
            width = Math.Max(1, (int)Math.Round(frameSize.Width));
            height = Math.Max(1, (int)Math.Round(frameSize.Height));
        }

        windowState = ToWpfWindowState(window.WindowState);

        RaisePropertyChanged(nameof(Left));
        RaisePropertyChanged(nameof(Top));
        RaisePropertyChanged(nameof(Width));
        RaisePropertyChanged(nameof(Height));
        RaisePropertyChanged(nameof(WindowState));
    }

    private static AvaloniaWindowState ToAvaloniaWindowState(WpfWindowState state)
        => state switch
        {
            WpfWindowState.Maximized => AvaloniaWindowState.Maximized,
            WpfWindowState.Minimized => AvaloniaWindowState.Minimized,
            _ => AvaloniaWindowState.Normal
        };

    private static WpfWindowState ToWpfWindowState(AvaloniaWindowState state)
        => state switch
        {
            AvaloniaWindowState.Maximized => WpfWindowState.Maximized,
            AvaloniaWindowState.Minimized => WpfWindowState.Minimized,
            _ => WpfWindowState.Normal
        };

    private static AvaloniaWindowStartupLocation ToAvaloniaWindowStartupLocation(WpfWindowStartupLocation state)
        => state switch
        {
            WpfWindowStartupLocation.CenterOwner => AvaloniaWindowStartupLocation.CenterOwner,
            WpfWindowStartupLocation.CenterScreen => AvaloniaWindowStartupLocation.CenterScreen,
            _ => AvaloniaWindowStartupLocation.Manual
        };

    private static WpfWindowStartupLocation ToWpfWindowStartupLocation(AvaloniaWindowStartupLocation state)
        => state switch
        {
            AvaloniaWindowStartupLocation.CenterOwner => WpfWindowStartupLocation.CenterOwner,
            AvaloniaWindowStartupLocation.CenterScreen => WpfWindowStartupLocation.CenterScreen,
            _ => WpfWindowStartupLocation.Manual
        };

    private static AvaloniaWindow ResolveOwnerWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private static void RunOnUiThread(Action action)
    {
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    private void CompleteLifecycleSignals()
    {
        if (lifecycleSignalsCompleted)
        {
            return;
        }

        lifecycleSignalsCompleted = true;
        whenLoaded.OnCompleted();
        whenClosed.OnCompleted();
        whenClosing.OnCompleted();
        whenActivated.OnCompleted();
        whenDeactivated.OnCompleted();
    }

    private const uint WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 2;
    private sealed record ManualResizeSession(WindowResizeDirection Direction, DrawingPoint StartPoint, DrawingRectangle StartRect);
    [DoNotNotify]
    private sealed class AvaloniaHostWindow : AvaloniaWindow
    {
        public void SetFrameSize(global::Avalonia.Size size)
        {
            FrameSize = size;
        }

        public void SetClientSize(global::Avalonia.Size size)
        {
            ClientSize = size;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

}
