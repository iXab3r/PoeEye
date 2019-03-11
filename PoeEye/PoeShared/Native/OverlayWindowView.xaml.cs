using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public partial class OverlayWindowView
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayWindowView));

        public OverlayWindowView()
        {
            InitializeComponent();

            WhenLoaded.Subscribe(OnLoaded);
            SizeChanged += OnSizeChanged;
            LocationChanged += OnLocationChanged;
        }

        public IObservable<EventPattern<RoutedEventArgs>> WhenLoaded => Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => Loaded += h, h => Loaded -= h);

        public IObservable<EventPattern<EventArgs>> WhenRendered => Observable
            .FromEventPattern<EventHandler, EventArgs>(h => ContentRendered += h, h => ContentRendered -= h);

        private void OnLocationChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            var windowViewModel = window?.DataContext as OverlayWindowViewModel;
            var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
            if (overlayViewModel == null)
            {
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            var window = sender as Window;
            var windowViewModel = window?.DataContext as OverlayWindowViewModel;
            var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
            if (overlayViewModel == null)
            {
                return;
            }

            var delta = sizeChangedEventArgs.NewSize.Height - sizeChangedEventArgs.PreviousSize.Height;
            if (!overlayViewModel.GrowUpwards)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() => Top -= delta), DispatcherPriority.Render);
        }

        private void OnLoaded()
        {
            var helper = new WindowInteropHelper(this);
            WindowsServices.SetWindowExNoActivate(helper.Handle);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public override string ToString()
        {
            return $"[OverlayWindow] DataContext: {DataContext} (X:{Left} Y:{Top} Width: {Width} Height: {Height})";
        }

        public void SetOverlayMode(OverlayMode mode)
        {
            switch (mode)
            {
                case OverlayMode.Layered:
                    MakeLayered();
                    break;
                case OverlayMode.Transparent:
                    MakeTransparent();
                    break;
            }
        }

        private void ThumbResize_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var window = thumb?.Tag as Window;

            if (thumb == null || window == null || e == null)
            {
                return;
            }

            try
            {
                if (SizeToContent != SizeToContent.Width)
                {
                    var newWidth = window.Width + e.HorizontalChange;
                    newWidth = Math.Min(newWidth, window.MaxWidth);
                    newWidth = Math.Max(newWidth, window.MinWidth);
                    window.Width = newWidth;
                }

                if (SizeToContent != SizeToContent.Height)
                {
                    var newHeight = window.Height + e.VerticalChange;
                    newHeight = Math.Min(newHeight, window.MaxHeight);
                    newHeight = Math.Max(newHeight, window.MinHeight);
                    window.Height = newHeight;
                }
            }
            catch (Exception exception)
            {
                Log.HandleUiException(exception);
            }
        }

        private void OverlayChildWindow_OnSizeChanged(object sender, SizeChangedEventArgs sizeInfo)
        {
            var window = sender as Window;
            var windowViewModel = window?.DataContext as OverlayWindowViewModel;
            var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
            if (window == null || windowViewModel == null || overlayViewModel == null || sizeInfo == null)
            {
                return;
            }

            try
            {
                if (sizeInfo.HeightChanged)
                {
                    overlayViewModel.ActualHeight = sizeInfo.NewSize.Height;
                }

                if (sizeInfo.WidthChanged)
                {
                    overlayViewModel.ActualWidth = sizeInfo.NewSize.Width;
                }
            }
            catch (Exception exception)
            {
                Log.HandleUiException(exception);
            }
        }
    }
}