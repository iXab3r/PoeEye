using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DynamicData.Binding;

using log4net;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using PropertyChanged;
using ReactiveUI;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
    {
        private static readonly Binder<OverlayViewModelBase> Binder = new();
        private static long GlobalWindowId = 0;

        private readonly ObservableAsPropertyHelper<PointF> dpi;
        private readonly object gate = new();
        private readonly CommandWrapper lockWindowCommand;
        private readonly CommandWrapper makeLayeredCommand;
        private readonly CommandWrapper makeTransparentCommand;
        private readonly Dispatcher uiDispatcher;
        private readonly CommandWrapper unlockWindowCommand;
        private readonly ISubject<Unit> whenLoaded = new ReplaySubject<Unit>(1);
        private bool isLocked = true;

        private readonly long windowId = Interlocked.Increment(ref GlobalWindowId);

        static OverlayViewModelBase()
        {
            Binder.BindAction(x => x.Log.Info($"Title updated to {x.Title}"));
        }

        protected OverlayViewModelBase()
        {
            Log = typeof(OverlayViewModelBase).PrepareLogger().WithSuffix(this.ToString).WithSuffix($"#{windowId}");
            Title = GetType().ToString();
            uiDispatcher = Dispatcher.CurrentDispatcher;
            Log.Info("Created new overlay window");

            lockWindowCommand = CommandWrapper.Create(LockWindowCommandExecuted, LockWindowCommandCanExecute);
            unlockWindowCommand = CommandWrapper.Create(UnlockWindowCommandExecuted, UnlockWindowCommandCanExecute);
            makeLayeredCommand = CommandWrapper.Create(MakeLayeredCommandExecuted, MakeLayeredCommandCanExecute);
            makeTransparentCommand = CommandWrapper.Create(MakeTransparentCommandExecuted, MakeTransparentCommandCanExecute);
            
            dpi = this.WhenAnyValue(x => x.OverlayWindow).Select(x => x == null ? Observable.Return(new PointF(1, 1)) : x.Observe(ConstantAspectRatioWindow.DpiProperty).Select(_ => OverlayWindow.Dpi).StartWith(OverlayWindow.Dpi))
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
                .ToUnit()
                .SubscribeSafe(whenLoaded)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NativeBounds)
                .CombineLatest(this.WhenAnyValue(x => x.OverlayWindow).Select(x => x?.WindowHandle), (targetBounds, hwnd) => new { TargetBounds = targetBounds, hwnd })
                .SubscribeSafe(x =>
                {
                    if (x.hwnd == null)
                    {
                        // window is not yet initialized
                        return;
                    }
                    
                    // WARNING - SetWindowRect is blocking as it awaits for WndProc to process the corresponding WM_* messages
                    Log.Info(() => $"Native bounds changed, setting windows rect: {NativeBounds} = {x.TargetBounds}");
                    UnsafeNative.SetWindowRect(x.hwnd.Value, x.TargetBounds);
                    var actualBounds = UnsafeNative.GetWindowRect(x.hwnd.Value);
                    Log.Info(() => $"Native bounds changed to {actualBounds} (expected {x.TargetBounds}), native: {NativeBounds}");

                }, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.OverlayWindow)
                .Where(x => x != null)
                .SubscribeSafe(x =>
                {
                    var hwndSource = HwndSource.FromHwnd(x.WindowHandle).AddTo(Anchors);
                    //Callback will happen on a OverlayWindow UI thread, usually it's app main UI thread
                    hwndSource.AddHook(WndProc);
                }, Log.HandleUiException)
                .AddTo(Anchors);
            
            Binder.Attach(this).AddTo(Anchors);
        }

        protected IObservable<Unit> WhenLoaded => whenLoaded;
        
        protected IFluentLog Log { get; }

        public bool GrowUpwards { get; set; }

        public Size DefaultSize { get; set; }

        public string OverlayDescription => $"{(OverlayWindow == null ? "NOWINDOW" : OverlayWindow.Name)}";

        public float Opacity { get; set; }

        public double? TargetAspectRatio { get; set; }

        public Point ViewModelLocation { get; set; }

        public ICommand UnlockWindowCommand => unlockWindowCommand;

        public ICommand MakeLayeredCommand => makeLayeredCommand;

        public ICommand MakeTransparentCommand => makeTransparentCommand;

        public ICommand LockWindowCommand => lockWindowCommand;

        public double ActualHeight { get; set; }

        public TransparentWindow OverlayWindow { get; private set; }

        public bool IsVisible { get; set; } = true;

        public double ActualWidth { get; set; }

        public PointF Dpi => dpi.Value;

        public Rectangle NativeBounds { get; set; }

        public Size MinSize { get; set; } = new Size(0, 0);

        public Size MaxSize { get; set; } = new Size(Int16.MaxValue, Int16.MaxValue);

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
            var interopHelper = new WindowInteropHelper(OverlayWindow);
            Log.Debug(() => $"Loaded overlay window: {OverlayWindow} ({interopHelper.Handle.ToHexadecimal()})");
        }

        private IntPtr WndProc(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var msg = (User32.WindowMessage)msgRaw;
            if (msg == User32.WindowMessage.WM_WINDOWPOSCHANGED && lParam != IntPtr.Zero)
            {
                var nativeStruct = Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS));
                if (nativeStruct != null)
                {
                    var wp = (UnsafeNative.WINDOWPOS)nativeStruct;
                    var bounds = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
                    if (NativeBounds != bounds)
                    {
                        Log.Info(() => $"Updating native bounds: {NativeBounds} => {bounds}");
                        NativeBounds = bounds;
                    }
                }
            }
            return IntPtr.Zero;
        }

        public override string ToString()
        {
            return $"{Title}";
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
            
            if (!overlayBounds.IsNotEmpty() || overlayBounds.IsNotEmpty() && UnsafeNative.IsOutOfBounds(overlayBounds, systemInformation.VirtualScreen))
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

        protected TRet RaiseAndSetIfChangedOnDispatcher<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            uiDispatcher.VerifyAccess();
            return RaiseAndSetIfChanged<TRet>(ref backingField, newValue, propertyName);
        }
    }
}