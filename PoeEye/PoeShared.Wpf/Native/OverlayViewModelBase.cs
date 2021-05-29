using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DynamicData.Binding;

using log4net;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayViewModelBase));

        private readonly CommandWrapper lockWindowCommand;
        private readonly CommandWrapper unlockWindowCommand;
        
        private readonly CommandWrapper makeLayeredCommand;
        private readonly CommandWrapper makeTransparentCommand;
        private readonly ISubject<Unit> whenLoaded = new ReplaySubject<Unit>(1);
        private readonly ISubject<Rectangle> windowPositionSource = new ReplaySubject<Rectangle>(1);
        private readonly ObservableAsPropertyHelper<PointF> dpi;
        private readonly object gate = new();

        private double actualHeight;
        private double actualWidth;
        private bool growUpwards;
        private bool showInTaskbar;
        private bool isLocked = true;
        private bool isUnlockable;
        private bool enableHeader = true;
        private Size defaultSize;

        private Size maxSize = new Size(Int16.MaxValue, Int16.MaxValue);
        private Size minSize = new Size(0, 0);
        private float opacity;
        private Rectangle nativeBounds;

        private OverlayMode overlayMode;

        private SizeToContent sizeToContent = SizeToContent.Manual;
        private string title;

        private double? targetAspectRatio;

        private TransparentWindow overlayWindow;
        private Point viewModelLocation;

        private readonly Dispatcher uiDispatcher;

        protected OverlayViewModelBase()
        {
            Title = GetType().ToString();
            uiDispatcher = Dispatcher.CurrentDispatcher;

            lockWindowCommand = CommandWrapper.Create(LockWindowCommandExecuted, LockWindowCommandCanExecute);
            unlockWindowCommand = CommandWrapper.Create(UnlockWindowCommandExecuted, UnlockWindowCommandCanExecute);
            makeLayeredCommand = CommandWrapper.Create(MakeLayeredCommandExecuted, MakeLayeredCommandCanExecute);
            makeTransparentCommand = CommandWrapper.Create(MakeTransparentCommandExecuted, MakeTransparentCommandCanExecute);

            dpi = this.WhenAnyValue(x => x.OverlayWindow).Select(x => x == null ? Observable.Return(new PointF(1, 1)) : x.Observe(ConstantAspectRatioWindow.DpiProperty).Select(_ => overlayWindow.Dpi).StartWith(overlayWindow.Dpi))
                .Switch()
                .Do(x => Log.Debug($"[{this}] DPI updated to {x}"))
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
                .CombineLatest(this.WhenAnyValue(x => x.OverlayWindow).Select(x => x?.WindowHandle), (targetBounds, hwnd) => new { nativeBounds = targetBounds, hwnd })
                .SubscribeSafe(x =>
                {
                    if (x.hwnd == null)
                    {
                        // window is not yet initialized
                        return;
                    }
                    
                    // WARNING - SetWindowRect is blocking as it awaits for WndProc to process the corresponding WM_* messages
                    UnsafeNative.SetWindowRect(x.hwnd.Value, x.nativeBounds);
                }, Log.HandleUiException)
                .AddTo(Anchors);

            windowPositionSource
                .Where(x => x != nativeBounds)
                .ObserveOnDispatcher(DispatcherPriority.DataBind)
                .SubscribeSafe(x =>
                {
                    NativeBounds = x;
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
                    windowPositionSource.OnNext(new Rectangle(wp.x, wp.y, wp.cx, wp.cy));
                }
            }
            return IntPtr.Zero;
        }

        protected IObservable<Unit> WhenLoaded => whenLoaded;

        public bool GrowUpwards
        {
            get => growUpwards;
            set => this.RaiseAndSetIfChanged(ref growUpwards, value);
        }

        public float Opacity
        {
            get => opacity;
            set => this.RaiseAndSetIfChanged(ref opacity, value);
        }

        public double? TargetAspectRatio
        {
            get => targetAspectRatio;
            set => this.RaiseAndSetIfChanged(ref targetAspectRatio, value);
        }

        public Point ViewModelLocation 
        {
            get => viewModelLocation;
            set => this.RaiseAndSetIfChanged(ref viewModelLocation, value);
        }
        
        public ICommand UnlockWindowCommand => unlockWindowCommand;
        
        public ICommand MakeLayeredCommand => makeLayeredCommand;
        
        public ICommand MakeTransparentCommand => makeTransparentCommand;

        public ICommand LockWindowCommand => lockWindowCommand;

        public double ActualHeight
        {
            get => actualHeight;
            set => this.RaiseAndSetIfChanged(ref actualHeight, value);
        }

        public TransparentWindow OverlayWindow
        {
            get => overlayWindow;
            private set => this.RaiseAndSetIfChanged(ref overlayWindow, value);
        }
        
        public double ActualWidth
        {
            get => actualWidth;
            set => this.RaiseAndSetIfChanged(ref actualWidth, value);
        }

        public PointF Dpi => dpi.Value;

        public Rectangle NativeBounds
        {
            get => nativeBounds;
            set => RaiseAndSetIfChanged(ref nativeBounds, value);
        }

        public Size MinSize
        {
            get => minSize;
            set => this.RaiseAndSetIfChanged(ref minSize, value);
        }

        public Size MaxSize
        {
            get => maxSize;
            set => this.RaiseAndSetIfChanged(ref maxSize, value);
        }

        public Size DefaultSize
        {
            get => defaultSize;
            set => RaiseAndSetIfChanged(ref defaultSize, value);
        }

        public bool IsLocked
        {
            get => isLocked;
            private set
            {
                if (!value && !IsUnlockable)
                {
                    throw new InvalidOperationException($"Overlay {this} cannot be unlocked");
                }

                this.RaiseAndSetIfChanged(ref isLocked, value);
            }
        }

        public bool EnableHeader
        {
            get => enableHeader;
            set => this.RaiseAndSetIfChanged(ref enableHeader, value);
        }

        public bool IsUnlockable
        {
            get => isUnlockable;
            protected set => this.RaiseAndSetIfChanged(ref isUnlockable, value);
        }

        public bool ShowInTaskbar
        {
            get => showInTaskbar;
            set => this.RaiseAndSetIfChanged(ref showInTaskbar, value);
        }

        public OverlayMode OverlayMode
        {
            get => overlayMode;
            set => this.RaiseAndSetIfChanged(ref overlayMode, value);
        }

        public SizeToContent SizeToContent
        {
            get => sizeToContent;
            protected set => this.RaiseAndSetIfChanged(ref sizeToContent, value);
        }

        public string Title 
        {
            get => title;
            protected set => this.RaiseAndSetIfChanged(ref title, value);
        }

        public string OverlayDescription => $"{(overlayWindow == null ? "NOWINDOW" : overlayWindow.Name)}";

        public virtual void ResetToDefault()
        {
            if (overlayWindow == null)
            {
                throw new InvalidOperationException("Overlay window is not loaded yet");
            }
            var activeMonitor = UnsafeNative.GetMonitorInfo(overlayWindow);

            Log.Warn($"Resetting overlay bounds (screen: {activeMonitor}, currently @ {nativeBounds})");
            var center = UnsafeNative.GetPositionAtTheCenter(overlayWindow).ScaleToScreen(Dpi);
            var size = (defaultSize.IsNotEmpty() ? defaultSize : minSize).ScaleToScreen(Dpi);
            NativeBounds = new Rectangle(center, size);
            Log.Info($"Reconfigured overlay bounds (screen: {activeMonitor}, new @ {nativeBounds})");

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
            Log.Debug($"[#{this}] Loaded overlay window: {OverlayWindow} ({interopHelper.Handle.ToHexadecimal()})");
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
                    Log.Warn($"[{this}] Failed to execute operation on dispatcher", e);
                }
            });
        }

        protected virtual void ApplyConfig(IOverlayConfig config)
        {
            Log.Debug($"[{OverlayDescription}] Applying configuration of type ({config.GetType().FullName})");
           
            var desktopHandle = UnsafeNative.GetDesktopWindow();
            var systemInformation = new
            {
                MonitorCount = SystemInformation.MonitorCount, 
                VirtualScreen = SystemInformation.VirtualScreen,
                MonitorBounds = UnsafeNative.GetMonitorBounds(desktopHandle).ToWinRectangle(),
                MonitorInfo = UnsafeNative.GetMonitorInfo(desktopHandle)
            };

            Log.Debug($"[{OverlayDescription}] Current SystemInformation: {systemInformation}");


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
            config.OverlayBounds = nativeBounds;
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
            Log.Debug($"[{OverlayDescription}] Unlocking window @ {nativeBounds}");
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
            Log.Debug($"[{OverlayDescription}] Locking window @ {nativeBounds}");
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
            Log.Debug($"[{OverlayDescription}] Making overlay Layered");
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
            Log.Debug($"[{OverlayDescription}] Making overlay Transparent");
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