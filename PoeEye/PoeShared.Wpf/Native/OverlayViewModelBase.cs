using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Reactive.Concurrency;
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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

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

        WhenKeyDown = this.WhenAnyValue(x => x.OverlayWindow)
            .Select(window => window != null ? Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.KeyDown += h, h => window.KeyDown -= h).Select(x => x) : Observable.Empty<EventPattern<KeyEventArgs>>())
            .Switch();

        this.WhenAnyValue(x => x.IsLocked)
            .Select(isLocked => isLocked == false ? WhenKeyDown : Observable.Empty<EventPattern<KeyEventArgs>>())
            .Switch()
            .SubscribeSafe(x =>
            {
                if (x.EventArgs.Key is Key.Down or Key.S)
                {
                    var bounds = NativeBounds;
                    NativeBounds = new Rectangle(bounds.X, bounds.Y + 1, bounds.Width, bounds.Height);
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Up or Key.W)
                {
                    var bounds = NativeBounds;
                    NativeBounds = new Rectangle(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height);
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Left or Key.A)
                {
                    var bounds = NativeBounds;
                    NativeBounds = new Rectangle(bounds.X - 1, bounds.Y, bounds.Width, bounds.Height);
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Right or Key.D)
                {
                    var bounds = NativeBounds;
                    NativeBounds = new Rectangle(bounds.X + 1, bounds.Y, bounds.Width, bounds.Height);
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.R)
                {
                    ResetToDefault();
                    x.EventArgs.Handled = true;
                }
            }, Log.HandleUiException)
            .AddTo(Anchors);

        Log.Info("Initialized overlay view model");

        this.WhenAnyValue(x => x.OverlayWindow)
            .SwitchIfNotDefault(x => x.Observe(ConstantAspectRatioWindow.NativeBoundsProperty, y => y.NativeBounds).Select(y => new { Window = x, ActualBounds = y }))
            .Subscribe(x =>
            {
                // always on UI thread
                Log.Debug(() => $"Updating {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
                NativeBounds = x.ActualBounds;
                Log.Debug(() => $"Updated {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
            })
            .AddTo(Anchors); 

        this.WhenAnyValue(x => x.OverlayWindow)
            .SwitchIfNotDefault(x => this.WhenAnyValue(y => y.NativeBounds)
                .Select(y => new { Window = x, DesiredBounds = y })
                .ObserveOnIfNeeded(x.Dispatcher))
            .Subscribe(x =>
            {
                // always on UI thread, possible recursive assignment
                // Native => SetWindowRect => Actual => Native => ...
                var overlayBounds = x.Window.NativeBounds;
                Log.Debug(() => $"Updating Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
                x.Window.NativeBounds = x.DesiredBounds;
                Log.Debug(() => $"Updated Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
            })
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    protected IObservable<Unit> WhenLoaded => whenLoaded;

    protected IFluentLog Log { get; }

    public bool GrowUpwards { get; set; }

    public Size DefaultSize { get; set; }

    public string OverlayDescription => $"{(OverlayWindow == null ? "NOWINDOW" : OverlayWindow.Name)}";

    public float Opacity { get; set; }

    public double? TargetAspectRatio { get; set; }

    public System.Windows.Point ViewModelLocation { get; set; }

    public ICommand UnlockWindowCommand => unlockWindowCommand;

    public ICommand MakeLayeredCommand => makeLayeredCommand;

    public ICommand MakeTransparentCommand => makeTransparentCommand;

    public ICommand LockWindowCommand => lockWindowCommand;

    public TransparentWindow OverlayWindow { get; private set; }

    public IObservable<EventPattern<KeyEventArgs>> WhenKeyDown { get; }

    public bool IsVisible { get; set; } = true;

    public PointF Dpi => dpi.Value;

    public Rectangle NativeBounds { get; set; }

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
        var size = DefaultSize.IsNotEmpty() ? DefaultSize : MinSize;
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

        Log.Info(() => $"Assigning overlay window: {owner}");

        Log.Info(() => $"Syncing window parameters with view model");
        UnsafeNative.SetWindowRect(owner.WindowHandle, NativeBounds);

        OverlayWindow = owner;
        Log.Info(() => $"Overlay window is assigned: {OverlayWindow}");
    }

    public override string ToString()
    {
        return $"OverlayVM {Id} Bounds: {NativeBounds}";
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
            MonitorBounds = UnsafeNative.GetMonitorBounds(desktopHandle),
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
}