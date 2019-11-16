using System;
using System.Windows;
using System.Windows.Interop;

namespace PoeShared.Native
{
    public class TransparentWindow : ConstantAspectRatioWindow
    {
        protected TransparentWindow()
        {
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.HideSystemMenu(hwnd);
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