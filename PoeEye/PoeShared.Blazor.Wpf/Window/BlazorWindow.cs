using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.FileProviders;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Unity;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow : DisposableReactiveObjectWithLogger, IBlazorWindow, IBlazorWindowMetroController
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
    private readonly PropertyValueHolder<WindowState> windowState;
    private readonly PropertyValueHolder<bool> noActivate;

    private readonly BlockingCollection<IWindowEvent> eventQueue;
    private readonly IScheduler uiScheduler;
    private readonly ManualResetEventSlim isClosed = new(false);
    private readonly SerialDisposable dragAnchor;
    private readonly ComplexFileProvider complexFileProvider;
    private readonly SerialDisposable additionalFileProviderAnchor;

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
        dragAnchor = new SerialDisposable().AddTo(Anchors);
        complexFileProvider = new ComplexFileProvider().AddTo(Anchors);
        additionalFileProviderAnchor = new SerialDisposable().AddTo(Anchors);

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
        noActivate = new PropertyValueHolder<bool>(this, nameof(NoActivate)).AddTo(Anchors);
        windowState = new PropertyValueHolder<WindowState>(this, nameof(WindowState)).AddTo(Anchors);

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
    
    public IDisposable RegisterFileProvider(IFileProvider fileProvider)
    {
        return complexFileProvider.Add(fileProvider);
    }

    public bool ShowInTaskbar
    {
        get => showInTaskbar.State.Value;
        set => showInTaskbar.SetValue(value, TrackedPropertyUpdateSource.External);
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
    public IObservable<EventArgs> WhenLoaded { get; }
    public IObservable<EventArgs> WhenUnloaded { get; }
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
    public event EventHandler Unloaded;
    public event EventHandler Closed;
    
    public void ShowDevTools()
    {
        Log.Debug("Enqueueing ShowDevTools command");
        EnqueueUpdate(new ShowDevToolsCommand());
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

    public IDisposable EnableDragMove()
    {
        EnsureNotDisposed();
        Log.Debug("Starts dragging the window");
        var anchor = new CompositeDisposable();
        EnqueueUpdate(new StartDragCommand(anchor));
        return anchor;
    }

    public IntPtr GetWindowHandle()
    {
        var window = GetWindow();
        return window.WindowHandle;
    }

    public ReactiveMetroWindowBase GetWindow()
    {
        EnsureNotDisposed();
        if (!windowSupplier.IsValueCreated)
        {
            throw new InvalidOperationException("Window is not created yet");
        }

        return windowSupplier.Value;
    }

    public void EnsureCreated()
    {
        if (uiScheduler.IsOnScheduler())
        {
            HandleUpdate();
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public Rectangle GetWindowRect()
    {
        EnsureNotDisposed();
        return new Rectangle(Left, Top, Width, Height);
    }
    
    public void SetWindowRect(Rectangle rect)
    {
        EnsureNotDisposed();
        Log.Debug($"Setting window rect to {rect} from {new Rectangle(Left, Top, Width, Height)}");
        windowLeft.SetValue(rect.Left, TrackedPropertyUpdateSource.Internal);
        windowTop.SetValue(rect.Top, TrackedPropertyUpdateSource.Internal);
        windowWidth.SetValue(rect.Width, TrackedPropertyUpdateSource.Internal);
        windowHeight.SetValue(rect.Height, TrackedPropertyUpdateSource.Internal);
        EnqueueUpdate(new SetWindowRectCommand(rect));
    }

    public void SetWindowSize(Size windowSize)
    {
        EnsureNotDisposed();
        Log.Debug($"Resizing window to {windowSize} from {new Size(Width, Height)}");
        windowWidth.SetValue(windowSize.Width, TrackedPropertyUpdateSource.Internal);
        windowHeight.SetValue(windowSize.Height, TrackedPropertyUpdateSource.Internal);
        EnqueueUpdate(new SetWindowSizeCommand(windowSize));
    }

    public void SetWindowPos(Point windowPos)
    {
        EnsureNotDisposed();
        Log.Debug($"Moving window to {windowPos} from {new Point(Left, Top)}");
        windowLeft.SetValue(windowPos.X, TrackedPropertyUpdateSource.Internal);
        windowTop.SetValue(windowPos.Y, TrackedPropertyUpdateSource.Internal);
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
            HandleEvent(windowEvent);
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
}