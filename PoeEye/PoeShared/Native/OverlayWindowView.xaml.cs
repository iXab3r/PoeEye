using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using PoeShared.Scaffolding;
using Xceed.Wpf.Toolkit;

namespace PoeShared.Native
{
    /// <summary>
    ///     Interaction logic for Window1.xaml
    /// </summary>
    public partial class OverlayWindowView
    {
        private readonly OverlayMode overlayMode;


        public OverlayWindowView(OverlayMode overlayMode = OverlayMode.Transparent)
        {
            this.overlayMode = overlayMode;
            InitializeComponent();
        }

        public IObservable<Unit> WhenLoaded => Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => this.Loaded += h, h => this.Loaded -= h)
            .ToUnit();

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            switch (overlayMode)
            {
                case OverlayMode.Layered:
                    MakeLayered();
                    break;
                default:
                    MakeTransparent();
                    break;
            }
        }
        
        public override string ToString()
        {
            return $"Overlay({overlayMode})";
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