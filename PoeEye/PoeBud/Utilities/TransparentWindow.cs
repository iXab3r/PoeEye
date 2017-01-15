using System;
using System.Windows;
using System.Windows.Interop;

namespace PoeBud.Utilities
{
    public class TransparentWindow : Window
    {
        public TransparentWindow()
        {
            this.WindowState = WindowState.Maximized;
            this.ShowActivated = true;
            this.WindowStyle = WindowStyle.None;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ResizeMode = ResizeMode.NoResize;
            this.IsTabStop = true;
            this.Focusable = true; 
            Visibility = Visibility.Collapsed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }
    }
}