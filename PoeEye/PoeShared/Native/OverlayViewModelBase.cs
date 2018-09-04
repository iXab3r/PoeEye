using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Windows;
using Guards;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
    {
        private double actualHeight;

        private double actualWidth;

        private bool growUpwards;

        private double height;
        private bool isLocked = true;

        private double left;
        private Size maxSize = new Size(double.NaN, double.NaN);
        private Size minSize = new Size(0, 0);

        private OverlayMode overlayMode;

        private SizeToContent sizeToContent = SizeToContent.Manual;

        private double top;

        private double width;
        private bool isUnlockable = false;
        private float opacity;

        public ISubject<Unit> WhenLoaded { get; } = new ReplaySubject<Unit>(1);

        public bool GrowUpwards
        {
            get => growUpwards;
            set => this.RaiseAndSetIfChanged(ref growUpwards, value);
        }

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
            set
            {
                if (!value && !IsUnlockable)
                {
                    throw new InvalidOperationException($"Overlay {this} is cannot be unlocked");
                }
                this.RaiseAndSetIfChanged(ref isLocked, value);
            }
        }

        public bool IsUnlockable
        {
            get => isUnlockable;
            set => this.RaiseAndSetIfChanged(ref isUnlockable, value);
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
            set => this.RaiseAndSetIfChanged(ref sizeToContent, value);
        }
        
        public float Opacity
        {
            get => opacity;
            set => this.RaiseAndSetIfChanged(ref opacity, value);
        }

        public virtual IOverlayViewModel SetActivationController(IActivationController controller)
        {
            Guard.ArgumentNotNull(controller, nameof(controller));

            return this;
        }

        protected void ApplyConfig(IOverlayConfig config)
        {
            if (config.OverlaySize.Height <= 0 || config.OverlaySize.Width <= 0)
            {
                IsLocked = false;
                config.OverlaySize = MinSize;
            }
            Width = config.OverlaySize.Width;
            Height = config.OverlaySize.Height;

            if (config.OverlayLocation.X <= 1 && config.OverlayLocation.Y <= 1)
            {
                IsLocked = false;
                config.OverlayLocation = new Point(Width / 2, Height / 2);
            }
            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;
            
            if (config.OverlayOpacity <= 0.01)
            {
                IsLocked = false;
                config.OverlayOpacity = 1;
            }
            Opacity = config.OverlayOpacity;
        }

        protected void SaveConfig(IOverlayConfig config)
        {
            config.OverlayLocation = new Point(Left, Top);
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
        }
    }
}