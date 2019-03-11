using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Common.Logging;
using Guards;
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
        private double actualHeight;

        private double actualWidth;

        private bool growUpwards;

        private double height;
        private bool isLocked = true;
        private bool isUnlockable;

        private double left;
        private Size maxSize = new Size(double.NaN, double.NaN);
        private Size minSize = new Size(0, 0);
        private float opacity;

        private OverlayMode overlayMode;

        private SizeToContent sizeToContent = SizeToContent.Manual;

        private double top;

        private double width;
        private Window overlayWindow;

        protected OverlayViewModelBase()
        {
            lockWindowCommand = CommandWrapper.Create(LockWindowCommandExecuted, LockWindowCommandCanExecute);
            unlockWindowCommand = CommandWrapper.Create(UnlockWindowCommandExecuted, UnlockWindowCommandCanExecute);
            this.WhenAnyValue(x => x.IsLocked, x => x.IsUnlockable)
                .Subscribe(() =>
                {
                    lockWindowCommand.RaiseCanExecuteChanged();
                    unlockWindowCommand.RaiseCanExecuteChanged();
                })
                .AddTo(Anchors);

            Title = GetType().ToString();
            WhenLoaded.Subscribe(OnLoaded).AddTo(Anchors);
        }

        private void OnLoaded(EventPattern<RoutedEventArgs> loadedArgs)
        {
            var window = loadedArgs.Sender as Window;
            if (window == null)
            {
                Log.Warn("Something went wrong - expected Window as a sender of Loaded event");
                return;
            }

            var interopHelper = new WindowInteropHelper(window);
            Log.Debug($"[#{this}] Loaded overlay window: {OverlayWindow} (0x{interopHelper.Handle.ToInt64():x8})");
            OverlayWindow = window;
        }

        protected ISubject<EventPattern<RoutedEventArgs>> WhenLoaded { get; } = new ReplaySubject<EventPattern<RoutedEventArgs>>(1);

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

        public ICommand UnlockWindowCommand => unlockWindowCommand;

        public ICommand LockWindowCommand => lockWindowCommand;

        public double ActualHeight
        {
            get => actualHeight;
            set => this.RaiseAndSetIfChanged(ref actualHeight, value);
        }

        public Window OverlayWindow
        {
            get => overlayWindow;
            set => this.RaiseAndSetIfChanged(ref overlayWindow, value);
        }
        
        public double ActualWidth
        {
            get => actualWidth;
            set => this.RaiseAndSetIfChanged(ref actualWidth, value);
        }

        public double Left
        {
            get => left;
            set => this.RaiseAndSetIfChanged(ref left, value);
        }

        public double Top
        {
            get => top;
            set => this.RaiseAndSetIfChanged(ref top, value);
        }

        public double Width
        {
            get => width;
            set => this.RaiseAndSetIfChanged(ref width, value);
        }

        public double Height
        {
            get => height;
            set => this.RaiseAndSetIfChanged(ref height, value);
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

        public bool IsUnlockable
        {
            get => isUnlockable;
            protected set => this.RaiseAndSetIfChanged(ref isUnlockable, value);
        }

        public OverlayMode OverlayMode
        {
            get => overlayMode;
            set => this.RaiseAndSetIfChanged(ref overlayMode, value);
        }

        IObservable<EventPattern<RoutedEventArgs>> IOverlayViewModel.WhenLoaded => WhenLoaded;

        public SizeToContent SizeToContent
        {
            get => sizeToContent;
            protected set => this.RaiseAndSetIfChanged(ref sizeToContent, value);
        }

        public string Title { get; protected set; }

        public virtual void ResetToDefault()
        {
            var activeMonitor = NativeMethods.GetMonitorInfo(OverlayWindow);

            Log.Warn($"Resetting overlay bounds (screen: {activeMonitor}, currently @ {new Rect(left, top, width, height)})");

            Width = MinSize.Width;
            Height = MinSize.Height;
            var center = GetPositionAtTheCenter();
            Left = center.X;
            Top = center.Y;
            Log.Info($"Reconfigured overlay bounds (screen: {activeMonitor}, new @ {new Rect(left, top, width, height)})");

            if (UnlockWindowCommand.CanExecute(null))
            {
                UnlockWindowCommand.Execute(null);
            }
        }

        private Point GetPositionAtTheCenter()
        {
            var monitorBounds = NativeMethods.GetActiveMonitorBounds(OverlayWindow);
            
            var screenCenter = new Point(
                monitorBounds.X + monitorBounds.Width / 2, 
                monitorBounds.Y + monitorBounds.Height / 2);
            screenCenter.Offset(- Width / 2, - Height / 2);

            return screenCenter;
        }

        public virtual void SetActivationController(IActivationController controller)
        {
            Guard.ArgumentNotNull(controller, nameof(controller));
        }
        
        protected void ApplyConfig(IOverlayConfig config)
        {
            Log.Debug($"Applying configuration: {config.DumpToTextRaw()}");
            if (config.OverlaySize.Height <= 0 ||
                config.OverlaySize.Width <= 0 ||
                double.IsNaN(config.OverlaySize.Height) ||
                double.IsNaN(config.OverlaySize.Width))
            {
                Log.Warn($"Overlay size is invalid, resetting to {MinSize}, config: {config.DumpToTextRaw()}");
                config.OverlaySize = MinSize;
                if (UnlockWindowCommand.CanExecute(null))
                {
                    UnlockWindowCommand.Execute(null);
                }
            }
            Width = config.OverlaySize.Width;
            Height = config.OverlaySize.Height;
            
            var systemInformation = new
            {
                MonitorCount = SystemInformation.MonitorCount, 
                VirtualScreen = SystemInformation.VirtualScreen,
                MonitorBounds = NativeMethods.GetActiveMonitorBounds(OverlayWindow),
                MonitorInfo = NativeMethods.GetMonitorInfo(OverlayWindow)
            };

            Log.Debug($"Current SystemInformation: {systemInformation.DumpToTextRaw()}");
            
            var overlayBounds = new Rect(config.OverlayLocation, config.OverlaySize);
            if (IsOutOfBounds(overlayBounds, systemInformation.MonitorBounds))
            {
                var screenCenter = GetPositionAtTheCenter();
                Log.Warn($"Overlay is out of screen bounds(screen: {systemInformation.MonitorBounds}, overlay: {overlayBounds}) , resetting to {screenCenter}, systemInfo: {systemInformation.DumpToTextRaw()}, config: {config.DumpToTextRaw()}");
                config.OverlayLocation = screenCenter;
                
                if (UnlockWindowCommand.CanExecute(null))
                {
                    UnlockWindowCommand.Execute(null);
                }
            }

            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;

            if (config.OverlayOpacity <= 0.01)
            {
                config.OverlayOpacity = 1;
                if (UnlockWindowCommand.CanExecute(null))
                {
                    UnlockWindowCommand.Execute(null);
                }
            }

            Opacity = config.OverlayOpacity;
        }

        private bool IsOutOfBounds(Point point, Size bounds)
        {
            return IsOutOfBounds(point, new Rect(new Point(), bounds));
        }
        
        private bool IsOutOfBounds(Rect frame, Rect bounds)
        {
            var downscaledFrame = frame;
            // downscaling frame as we do not require for FULL frame to be visible, only top-left part of it
            downscaledFrame.Size = frame.Size.Scale(0.5);
            
            return double.IsNaN(frame.X) ||
                   double.IsNaN(frame.Y) ||
                   double.IsNaN(frame.Width) ||
                   double.IsNaN(frame.Height) ||
                   downscaledFrame.X <= 1 ||
                   downscaledFrame.Y <= 1 ||
                   !bounds.Contains(downscaledFrame);
        }

        private bool IsOutOfBounds(Point point, Rect bounds)
        {
            return double.IsNaN(point.X) ||
                   double.IsNaN(point.Y) ||
                   point.X <= 1 ||
                   point.Y <= 1 ||
                   !bounds.IntersectsWith(new Rect(point.X, point.Y, 1, 1));
        }

        protected void SavePropertiesToConfig(IOverlayConfig config)
        {
            config.OverlayLocation = new Point(Left, Top);
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
        }

        protected virtual void UnlockWindowCommandExecuted()
        {
            if (!UnlockWindowCommandCanExecute())
            {
                throw new InvalidOperationException($"Unsupported operation in this state, overlay(IsLocked: {IsLocked}, IsUnlockable: {IsUnlockable}): {this.DumpToTextRaw()}");
            }
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
                throw new InvalidOperationException($"Unsupported operation in this state, overlay(IsLocked: {IsLocked}): {this.DumpToTextRaw()}");
            }
            IsLocked = true;
        }

        protected virtual bool LockWindowCommandCanExecute()
        {
            return !IsLocked;
        }
        
        internal static class NativeMethods
        {
            [DllImport("user32.dll", EntryPoint = "GetDC")]
            private static extern IntPtr GetDC(IntPtr ptr);

            public static Rect GetActiveMonitorBounds(Window window)
            {
                var handle = window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;
                return GetActiveMonitorBounds(handle);
            }

            public static Rect GetActiveMonitorBounds(IntPtr windowHandle)
            {
                var screen = Screen.FromHandle(windowHandle);
                var graphics = Graphics.FromHdc(GetDC(windowHandle));

                Log.Debug($"Monitor for window 0x{windowHandle.ToInt64():X8}: {GetMonitorInfo(windowHandle)}");
                return GetMonitorBounds(screen, graphics);
            }

            public static Rect GetMonitorBounds(Screen monitor, Graphics graphics)
            {
                var result = new Rect(
                    monitor.Bounds.X,
                    monitor.Bounds.Y,
                    monitor.Bounds.Width,
                    monitor.Bounds.Height);
                result.Scale(96 / graphics.DpiX, 96f / graphics.DpiY);
                return result;
            }

            public static string GetMonitorInfo(Window window)
            {
                var handle = window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;
                return GetMonitorInfo(handle);
            }

            public static string GetMonitorInfo(IntPtr windowHandle)
            {
                var screen = Screen.FromHandle(windowHandle);
                var graphics = Graphics.FromHdc(GetDC(windowHandle));
                var scaledBounds = GetMonitorBounds(screen, graphics);
                return new
                {
                    screen.DeviceName, screen.Primary, graphics.PageScale, SystemBounds = screen.Bounds, ScaledBounds = scaledBounds, graphics.DpiX, graphics.DpiY, 
                }.DumpToTextRaw();
            }
        }
    }
}