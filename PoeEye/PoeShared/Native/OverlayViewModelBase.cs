using System.Windows;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
    {
        private object header;

        private double height;
        private bool isLocked = true;
        private Point location;
        private Size maxSize = new Size(double.NaN, double.NaN);
        private Size minSize = new Size(0, 0);

        private double width;

        public Point Location
        {
            get { return location; }
            set { this.RaiseAndSetIfChanged(ref location, value); }
        }

        public double Width
        {
            get { return width; }
            set { this.RaiseAndSetIfChanged(ref width, value); }
        }

        public double Height
        {
            get { return height; }
            set { this.RaiseAndSetIfChanged(ref height, value); }
        }

        public Size MinSize
        {
            get { return minSize; }
            set { this.RaiseAndSetIfChanged(ref minSize, value); }
        }

        public Size MaxSize
        {
            get { return maxSize; }
            set { this.RaiseAndSetIfChanged(ref maxSize, value); }
        }

        public bool IsLocked
        {
            get { return isLocked; }
            set { this.RaiseAndSetIfChanged(ref isLocked, value); }
        }

        public object Header
        {
            get { return header; }
            set { this.RaiseAndSetIfChanged(ref header, value); }
        }
    }
}