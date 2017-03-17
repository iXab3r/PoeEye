using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace PoeShared.Native
{
    public class TransparentWindow : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

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