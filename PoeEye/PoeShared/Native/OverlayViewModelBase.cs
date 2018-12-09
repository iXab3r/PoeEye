using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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
        }

        protected ISubject<Unit> WhenLoaded { get; } = new ReplaySubject<Unit>(1);

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

        IObservable<Unit> IOverlayViewModel.WhenLoaded => WhenLoaded;

        public SizeToContent SizeToContent
        {
            get => sizeToContent;
            protected set => this.RaiseAndSetIfChanged(ref sizeToContent, value);
        }

        public string Title { get; protected set; }

        public virtual void SetActivationController(IActivationController controller)
        {
            Guard.ArgumentNotNull(controller, nameof(controller));
        }

        protected void ApplyConfig(IOverlayConfig config)
        {
            if (config.OverlaySize.Height <= 0 ||
                config.OverlaySize.Width <= 0 ||
                double.IsNaN(config.OverlaySize.Height) ||
                double.IsNaN(config.OverlaySize.Width))
            {
                UnlockWindowCommand.Execute(null);
                config.OverlaySize = MinSize;
            }

            Width = config.OverlaySize.Width;
            Height = config.OverlaySize.Height;

            if (config.OverlayLocation.X <= 1 ||
                config.OverlayLocation.Y <= 1 ||
                double.IsNaN(config.OverlayLocation.X) ||
                double.IsNaN(config.OverlayLocation.Y))
            {
                UnlockWindowCommand.Execute(null);
                var size = NativeMethods.GetPrimaryScreenSize();
                config.OverlayLocation = new Point(size.Width / 2, size.Height / 2);
            }

            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;

            if (config.OverlayOpacity <= 0.01)
            {
                UnlockWindowCommand.Execute(null);
                config.OverlayOpacity = 1;
            }

            Opacity = config.OverlayOpacity;
        }

        protected void SavePropertiesToConfig(IOverlayConfig config)
        {
            config.OverlayLocation = new Point(Left, Top);
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
        }

        protected virtual void UnlockWindowCommandExecuted()
        {
            IsLocked = false;
        }

        protected virtual bool UnlockWindowCommandCanExecute()
        {
            return IsUnlockable && IsLocked;
        }

        protected virtual void LockWindowCommandExecuted()
        {
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

            public static Size GetPrimaryScreenSize()
            {
                var graphics = Graphics.FromHdc(GetDC(IntPtr.Zero));
                return new Size(
                    SystemInformation.PrimaryMonitorSize.Width / graphics.DpiX,
                    SystemInformation.PrimaryMonitorSize.Height / graphics.DpiY);
            }
        }
    }
}