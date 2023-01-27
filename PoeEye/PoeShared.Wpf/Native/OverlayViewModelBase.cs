using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using PInvoke;
using PoeShared.Scaffolding;
using PropertyBinder;
using PropertyChanged;
using ReactiveUI;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Drawing.Point;

namespace PoeShared.Native;

public abstract class OverlayViewModelBase : WindowViewModelBase, IOverlayViewModel
{
    private static readonly Binder<OverlayViewModelBase> Binder = new();

    private readonly CommandWrapper lockWindowCommand;
    private readonly CommandWrapper makeLayeredCommand;
    private readonly CommandWrapper makeTransparentCommand;
    private readonly CommandWrapper unlockWindowCommand;

    private bool isLocked = true;

    static OverlayViewModelBase()
    {
    }

    protected OverlayViewModelBase()
    {
        Log.Info("Created overlay view model");

        lockWindowCommand = CommandWrapper.Create(LockWindowCommandExecuted, LockWindowCommandCanExecute);
        unlockWindowCommand = CommandWrapper.Create(UnlockWindowCommandExecuted, UnlockWindowCommandCanExecute);
        makeLayeredCommand = CommandWrapper.Create(MakeLayeredCommandExecuted, MakeLayeredCommandCanExecute);
        makeTransparentCommand = CommandWrapper.Create(MakeTransparentCommandExecuted, MakeTransparentCommandCanExecute);

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
       
        this.WhenAnyValue(x => x.IsLocked)
            .Select(isLocked => isLocked == false ? WhenKeyDown : Observable.Empty<EventPattern<KeyEventArgs>>())
            .Switch()
            .SubscribeSafe(x =>
            {
                if (x.EventArgs.Key is Key.Down or Key.S)
                {
                    var bounds = NativeBounds;
                    NativeBounds = bounds with {Y = bounds.Y + 1};
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Up or Key.W)
                {
                    var bounds = NativeBounds;
                    NativeBounds = bounds with {Y = bounds.Y - 1};
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Left or Key.A)
                {
                    var bounds = NativeBounds;
                    NativeBounds = bounds with {X = bounds.X - 1};
                    x.EventArgs.Handled = true;
                }
                else if (x.EventArgs.Key is Key.Right or Key.D)
                {
                    var bounds = NativeBounds;
                    NativeBounds = bounds with {X = bounds.X + 1};
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

        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    public bool GrowUpwards { get; set; }

    public float Opacity { get; set; }

    public System.Windows.Point ViewModelLocation { get; set; }

    public ICommand UnlockWindowCommand => unlockWindowCommand;

    public ICommand MakeLayeredCommand => makeLayeredCommand;

    public ICommand MakeTransparentCommand => makeTransparentCommand;

    public ICommand LockWindowCommand => lockWindowCommand;

    public bool ShowResizeThumbs { get; set; } = true;
    
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

    public bool IsUnlockable { get; protected set; } = true;

    public OverlayMode OverlayMode { get; set; }

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
        Log.Info(() => $"Reconfigured overlay bounds (screen: {activeMonitor}, new @ {NativeBounds})");

        if (UnlockWindowCommand.CanExecute(null))
        {
            UnlockWindowCommand.Execute(null);
        }
    }

    public virtual void SetActivationController(IActivationController controller)
    {
        Guard.ArgumentNotNull(controller, nameof(controller));
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

        var overlayBounds = config.OverlayBounds;
        if (!overlayBounds.IsNotEmptyArea() || overlayBounds.IsNotEmptyArea() && UnsafeNative.IsOutOfBounds(overlayBounds, systemInformation.VirtualScreen))
        {
            Log.Warn($"[{OverlayDescription}] Overlay is out of screen bounds(screen: {systemInformation.MonitorBounds}, overlay: {overlayBounds}) , resetting to position to screen center, systemInfo: {systemInformation.Dump()}, config: {config.Dump()}");
            ResetToDefault();
        }
        else
        {
            NativeBounds = overlayBounds;
        }

        if (config.OverlayOpacity <= 0.01)
        {
            Log.Warn($"[{OverlayDescription}] Overlay is fully invisible(screen: {systemInformation.MonitorBounds}, overlay: {overlayBounds}), systemInfo: {systemInformation}, config: {config.Dump()}");

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