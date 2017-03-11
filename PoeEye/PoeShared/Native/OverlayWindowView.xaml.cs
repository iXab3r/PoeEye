using System;
using System.Windows.Controls.Primitives;
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
            var window = thumb?.Tag as ChildWindow;

            if (thumb == null || window == null || e == null)
            {
                return;
            }

            try
            {
                var newWidth = window.ActualWidth + e.HorizontalChange;
                newWidth = Math.Min(newWidth, window.MaxWidth);
                newWidth = Math.Max(newWidth, window.MinWidth);
                window.Width = newWidth;

                var newHeight = window.ActualHeight + e.VerticalChange;
                newHeight = Math.Min(newHeight, window.MaxHeight);
                newHeight = Math.Max(newHeight, window.MinHeight);
                window.Height = newHeight;
            }
            catch (Exception exception)
            {
                Log.HandleUiException(exception);
            }
        }
    }
}