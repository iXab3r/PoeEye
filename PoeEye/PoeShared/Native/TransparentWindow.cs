using System;
using System.Windows;
using System.Windows.Interop;

namespace PoeShared.Native
{
    public class TransparentWindow : Window
    {
        public TransparentWindow()
        {
            //FIXME Hide from Alt+Tab
            this.WindowState = WindowState.Maximized;
            this.ShowActivated = true;
            this.WindowStyle = WindowStyle.None;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ResizeMode = ResizeMode.NoResize;
            this.IsTabStop = false;
            this.Focusable = true; 
            Visibility = Visibility.Collapsed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        protected void MakeTransparent()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        protected void MakeLayered()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExLayered(hwnd);
        }
    }
}