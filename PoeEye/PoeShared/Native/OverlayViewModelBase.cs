using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public abstract class OverlayViewModelBase : DisposableReactiveObject, IOverlayViewModel
    {
        private double actualHeight;

        private double actualWidth;

        private double height;
        private bool isLocked = true;

        private double left;
        private Size maxSize = new Size(double.NaN, double.NaN);
        private Size minSize = new Size(0, 0);

        private double top;

        private double width;

        public double ActualHeight
        {
            get { return actualHeight; }
            set { this.RaiseAndSetIfChanged(ref actualHeight, value); }
        }

        public double ActualWidth
        {
            get { return actualWidth; }
            set { this.RaiseAndSetIfChanged(ref actualWidth, value); }
        }

        public double Left
        {
            get { return left; }
            set { this.RaiseAndSetIfChanged(ref left, value); }
        }

        public double Top
        {
            get { return top; }
            set { this.RaiseAndSetIfChanged(ref top, value); }
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

        public ISubject<Unit> WhenLoaded { get; } = new ReplaySubject<Unit>(1);

        IObservable<Unit> IOverlayViewModel.WhenLoaded => this.WhenLoaded;
    }
}