using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DynamicData.Binding;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;
using PropertyChanged;
using ReactiveUI;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native;

public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
{
    private static readonly Binder<OverlayViewModelBase> Binder = new();
    private static long GlobalWindowId;

    private readonly ObservableAsPropertyHelper<PointF> dpi;
    private readonly CommandWrapper lockWindowCommand;
    private readonly CommandWrapper makeLayeredCommand;
    private readonly CommandWrapper makeTransparentCommand;
    private readonly Dispatcher uiDispatcher;
    private readonly CommandWrapper unlockWindowCommand;
    private readonly ISubject<Unit> whenLoaded = new ReplaySubject<Unit>(1);

    private bool isLocked = true;

    static OverlayViewModelBase()
    {
        Binder.BindAction(x => x.Log.Info($"Title updated to {x.Title}"));
    }

    protected OverlayViewModelBase()
    {
        Log = GetType().PrepareLogger().WithSuffix(Id).WithSuffix(ToString);
        Title = GetType().ToString();
        uiDispatcher = Dispatcher.CurrentDispatcher;
        Log.Info("Created overlay view model");

        lockWindowCommand = CommandWrapper.Create(LockWindowCommandExecuted, LockWindowCommandCanExecute);
        unlockWindowCommand = CommandWrapper.Create(UnlockWindowCommandExecuted, UnlockWindowCommandCanExecute);
        makeLayeredCommand = CommandWrapper.Create(MakeLayeredCommandExecuted, MakeLayeredCommandCanExecute);
        makeTransparentCommand = CommandWrapper.Create(MakeTransparentCommandExecuted, MakeTransparentCommandCanExecute);

        // this sync mechanism is needed to keep NativeBounds in sync with real current window position WITHOUT getting into recursive assignments
        // i.e. Real position changes => NativeBounds tries to sync, fails to do so due to rounding or any other mechanism => changes window bounds => real position changes...
        this.WhenAnyValue(x => x.WindowBounds)
            .ObserveOn(uiDispatcher)
            .Subscribe(x => RaisePropertyChanged(nameof(NativeBounds)))
            .AddTo(Anchors);
            
        dpi = this.WhenAnyValue(x => x.OverlayWindow).Select(x => x == null ? Observable.Return(new PointF(1, 1)) : x.Observe(ConstantAspectRatioWindow.DpiProperty).Select(_ => OverlayWindow.Dpi))
            .Switch()
            .Do(x => Log.Debug(() => $"DPI updated to {x}"))
            .ToProperty(this, x => x.Dpi)
            .AddTo(Anchors);
            
        this.WhenAnyValue(x => x.IsLocked, x => x.IsUnlockable)
            .SubscribeSafe(() =>
            {
                lockWindowCommand.RaiseCanExecuteChanged();
                unlockWindowCommand.RaiseCanExecuteChanged();
            }, Log.HandleUiException)
            .AddTo(Anchors);
            
        this.WhenAnyValue(x => x.OverlayMode)
            .SubscribeSafe(() =>
            {
                makeLayeredCommand.RaiseCanExecuteChanged();
                makeTransparentCommand.RaiseCanExecuteChanged();
            }, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenValueChanged(x => x.OverlayWindow, false)
            .Take(1)
            .Select(x => x.WhenLoaded())
            .Switch()
            .SubscribeSafe(whenLoaded)
            .AddTo(Anchors);
        whenLoaded.SubscribeSafe(_ =>
        {
            if (IsLoaded)
            {
                Log.Warn("Window received multiple 'loaded' events");
                throw new ApplicationException($"Window has already been loaded: {this}");
            }
            Log.Debug("Window has been loaded, changing status");
            IsLoaded = true;
        }, Log.HandleUiException).AddTo(Anchors);

        this.WhenAnyValue(x => x.WindowBounds)
            .CombineLatest(this.WhenAnyValue(x => x.OverlayWindow).Select(x => x?.WindowHandle ?? IntPtr.Zero), (desiredBounds, hwnd) => new { DesiredBounds = desiredBounds, hwnd })
            .Where(x => x.hwnd != IntPtr.Zero && x.DesiredBounds.SourceType == ValueSourceType.User)
            .ObserveOn(uiDispatcher)
            .SubscribeSafe(x =>
            {
                // WARNING - Get/SetWindowRect are blocking as they await for WndProc to process the corresponding WM_* messages
                Log.Info(() => $"Native bounds changed, setting windows rect: {WindowBounds} => {x.DesiredBounds}");
                UnsafeNative.SetWindowRect(x.hwnd, x.DesiredBounds.Value);
                var actualBounds = UnsafeNative.GetWindowRect(x.hwnd);
                Log.Info(() => $"Native bounds changed to RECT {actualBounds} (expected {x.DesiredBounds}), current: {WindowBounds})");
            }, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.OverlayWindow)
            .Where(x => x != null)
            .Take(1)
            .SubscribeSafe(x =>
            {
                Log.Debug(() => $"Overlay window is set, resolving {nameof(HwndSource)} for {x}");
                var hwndSource = HwndSource.FromHwnd(x.WindowHandle);
                if (hwndSource == null)
                {
                    throw new InvalidStateException($"Failed to resolve {nameof(HwndSource)} for {x}");
                }
                Disposable.Create(() =>
                {
                    Log.Debug(() => $"Releasing {nameof(HwndSource)} of {x}");
                    hwndSource.Dispose();
                }).AddTo(Anchors);
                //Callback will happen on a OverlayWindow UI thread, usually it's app main UI thread
                Log.Debug(() => $"Resolved {nameof(HwndSource)} for {x}: {hwndSource}");
                hwndSource.AddHook(WndProc);
            }, Log.HandleUiException)
            .AddTo(Anchors);
        Log.Info("Initialized overlay view model");

        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    protected IObservable<Unit> WhenLoaded => whenLoaded;

    protected IFluentLog Log { get; }

    public bool GrowUpwards { get; set; }

    public Size DefaultSize { get; set; }

    public string OverlayDescription => $"{(OverlayWindow == null ? "NOWINDOW" : OverlayWindow.Name)}";

    private ValueHolder<Rectangle> WindowBounds { get; set; }

    public float Opacity { get; set; }

    public double? TargetAspectRatio { get; set; }

    public Point ViewModelLocation { get; set; }

    public ICommand UnlockWindowCommand => unlockWindowCommand;

    public ICommand MakeLayeredCommand => makeLayeredCommand;

    public ICommand MakeTransparentCommand => makeTransparentCommand;

    public ICommand LockWindowCommand => lockWindowCommand;

    public TransparentWindow OverlayWindow { get; private set; }

    public bool IsVisible { get; set; } = true;

    public PointF Dpi => dpi.Value;

    [DoNotNotify]
    public Rectangle NativeBounds
    {
        get => WindowBounds.Value;
        set => WindowBounds = new ValueHolder<Rectangle>(value, ValueSourceType.User);
    }

    public Size MinSize { get; set; } = new Size(0, 0);

    public Size MaxSize { get; set; } = new Size(Int16.MaxValue, Int16.MaxValue);

    public bool IsLoaded { get; private set; }

    [DoNotNotify]
    public bool IsLocked
    {
        get => isLocked;
        set
        {
            if (!value && !IsUnlockable)
            {
                throw new InvalidOperationException($"Overlay {this} cannot be unlocked");
            }

            this.RaiseAndSetIfChanged(ref isLocked, value);
        }
    }

    public bool EnableHeader { get; set; } = true;

    public bool IsUnlockable { get; protected set; }

    public bool ShowInTaskbar { get; set; }

    public OverlayMode OverlayMode { get; set; }

    public SizeToContent SizeToContent { get; protected set; } = SizeToContent.Manual;

    public string Id { get; } = $"Overlay#{Interlocked.Increment(ref GlobalWindowId)}";

    public string Title { get; protected set; }

    public virtual void ResetToDefault()
    {
        if (OverlayWindow == null)
        {
            throw new InvalidOperationException("Overlay window is not loaded yet");
        }
        var activeMonitor = UnsafeNative.GetMonitorInfo(OverlayWindow);

        Log.Warn($"Resetting overlay bounds (screen: {activeMonitor}, currently @ {NativeBounds})");
        var center = UnsafeNative.GetPositionAtTheCenter(OverlayWindow).ScaleToScreen(Dpi);
        var size = (DefaultSize.IsNotEmpty() ? DefaultSize : MinSize).ScaleToScreen(Dpi);
        NativeBounds = new Rectangle(center, size);
        Log.Info($"Reconfigured overlay bounds (screen: {activeMonitor}, new @ {NativeBounds})");

        if (UnlockWindowCommand.CanExecute(null))
        {
            UnlockWindowCommand.Execute(null);
        }
    }

    public virtual void SetActivationController(IActivationController controller)
    {
        Guard.ArgumentNotNull(controller, nameof(controller));
    }

    public void SetOverlayWindow(TransparentWindow owner)
    {
        Guard.ArgumentNotNull(owner, nameof(owner));
            
        if (this.OverlayWindow != null)
        {
            throw new InvalidOperationException($"Window is already assigned");
        }
        OverlayWindow = owner;
        Log.Debug(() => $"Overlay window is assigned: {OverlayWindow}");
    }

    private IntPtr WndProc(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        //this callback is called on UI thread and handles all messages sent to window
        var msg = (User32.WindowMessage)msgRaw;
        if (msg == User32.WindowMessage.WM_WINDOWPOSCHANGED && lParam != IntPtr.Zero)
        {
            var nativeStruct = Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS));
            if (nativeStruct != null)
            {
                var wp = (UnsafeNative.WINDOWPOS)nativeStruct;
                var bounds = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
                var currentBounds = WindowBounds;
                if (currentBounds.Value != bounds)
                {
                    Log.Info(() => $"Updating native bounds: {NativeBounds} => {bounds}");
                    WindowBounds = new ValueHolder<Rectangle>(){ Value = bounds, SourceType = ValueSourceType.System };
                }
            }
        }
        return IntPtr.Zero;
    }

    public override string ToString()
    {
        return Id;
    }

    public DispatcherOperation BeginInvoke(Action dispatcherAction)
    {
        return uiDispatcher.BeginInvoke(() =>
        {
            try
            {
                dispatcherAction();
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to execute operation on dispatcher", e);
            }
        });
    }

    protected virtual void ApplyConfig(IOverlayConfig config)
    {
        Log.Debug(() => $"[{OverlayDescription}] Applying configuration of type ({config.GetType().FullName})");
           
        var desktopHandle = UnsafeNative.GetDesktopWindow();
        var systemInformation = new
        {
            MonitorCount = SystemInformation.MonitorCount, 
            VirtualScreen = SystemInformation.VirtualScreen,
            MonitorBounds = UnsafeNative.GetMonitorBounds(desktopHandle).ToWinRectangle(),
            MonitorInfo = UnsafeNative.GetMonitorInfo(desktopHandle)
        };

        Log.Debug(() => $"[{OverlayDescription}] Current SystemInformation: {systemInformation}");


        Rectangle overlayBounds;
        if (config.OverlayLocation != null && config.OverlaySize != null)
        {
            overlayBounds = new System.Windows.Rect(config.OverlayLocation.Value, config.OverlaySize.Value).ScaleToScreen(Dpi);
        }
        else
        {
            overlayBounds = config.OverlayBounds;
        }
            
        if (!overlayBounds.IsNotEmptyArea() || overlayBounds.IsNotEmptyArea() && UnsafeNative.IsOutOfBounds(overlayBounds, systemInformation.VirtualScreen))
        {
            Log.Warn($"[{OverlayDescription}] Overlay is out of screen bounds(screen: {systemInformation.MonitorBounds}, overlay: {overlayBounds}) , resetting to position to screen center, systemInfo: {systemInformation.DumpToTextRaw()}, config: {config.DumpToTextRaw()}");
            ResetToDefault();
        }
        else
        {
            NativeBounds = overlayBounds;
        }

        if (config.OverlayOpacity <= 0.01)
        {
            Log.Warn($"[{OverlayDescription}] Overlay is fully invisible(screen: {systemInformation.MonitorBounds}, overlay: {overlayBounds}), systemInfo: {systemInformation}, config: {config.DumpToTextRaw()}");

            config.OverlayOpacity = 1;
            if (UnlockWindowCommand.CanExecute(null))
            {
                UnlockWindowCommand.Execute(null);
            }
        }

        Opacity = config.OverlayOpacity;
    }

    protected void SavePropertiesToConfig(IOverlayConfig config)
    {
        config.OverlayBounds = NativeBounds;
        config.OverlayLocation = default;
        config.OverlaySize = default;
        config.OverlayOpacity = Opacity;
    }

    protected virtual void UnlockWindowCommandExecuted()
    {
        if (!UnlockWindowCommandCanExecute())
        {
            throw new InvalidOperationException($"[{OverlayDescription}] Unsupported operation in this state, overlay(IsLocked: {IsLocked}, IsUnlockable: {IsUnlockable}): {this}");
        }
        Log.Debug(() => $"[{OverlayDescription}] Unlocking window @ {NativeBounds}");
        IsLocked = false;
    }

    protected virtual bool UnlockWindowCommandCanExecute()
    {
        return IsUnlockable && IsLocked;
    }

    protected virtual void LockWindowCommandExecuted()
    {
        if (!LockWindowCommandCanExecute())
        {
            throw new InvalidOperationException($"[{OverlayDescription}] Unsupported operation in this state, overlay(IsLocked: {IsLocked}): {this}");
        }
        Log.Debug(() => $"[{OverlayDescription}] Locking window @ {NativeBounds}");
        IsLocked = true;
    }

    protected virtual bool LockWindowCommandCanExecute()
    {
        return !IsLocked;
    }

    protected virtual bool MakeLayeredCommandCanExecute()
    {
        return OverlayMode == OverlayMode.Transparent;
    }

    protected virtual void MakeLayeredCommandExecuted()
    {
        if (!MakeLayeredCommandCanExecute())
        {
            throw new InvalidOperationException($"[{OverlayDescription}] Unsupported operation in this state, overlay(OverlayMode: {OverlayMode}): {this}");
        }
        Log.Debug(() => $"[{OverlayDescription}] Making overlay Layered");
        OverlayMode = OverlayMode.Layered;
    }

    protected virtual bool MakeTransparentCommandCanExecute()
    {
        return OverlayMode == OverlayMode.Layered;
    }

    protected virtual void MakeTransparentCommandExecuted()
    {
        if (!MakeTransparentCommandCanExecute())
        {
            throw new InvalidOperationException($"[{OverlayDescription}] Unsupported operation in this state, overlay(OverlayMode: {OverlayMode}): {this}");
        }
        Log.Debug(() => $"[{OverlayDescription}] Making overlay Transparent");
        OverlayMode = OverlayMode.Transparent;
    }

    private readonly struct ValueHolder<T> : IEquatable<ValueHolder<T>>
    {
        public ValueHolder(T value, ValueSourceType sourceType = ValueSourceType.User)
        {
            Value = value;
            SourceType = sourceType;
        }

        public T Value { get; init; }
            
        public ValueSourceType SourceType { get; init; }

        public bool Equals(ValueHolder<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ValueHolder<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(ValueHolder<T> left, ValueHolder<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueHolder<T> left, ValueHolder<T> right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Value} (src: {SourceType})";
        }
    }

    private enum ValueSourceType
    {
        User,
        System
    }
}