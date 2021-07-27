using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace PoeShared.Native
{
    public class TransparentWindow : ConstantAspectRatioWindow
    {

        private readonly Lazy<IntPtr> windowHandleSupplier;

        public TransparentWindow()
        {
            windowHandleSupplier = new Lazy<IntPtr>(() => new WindowInteropHelper(this).EnsureHandle());
            this.Loaded += OnLoaded;
        }

        public IntPtr WindowHandle => windowHandleSupplier.Value;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UnsafeNative.HideSystemMenu(windowHandleSupplier.Value);
        }

        protected void MakeTransparent()
        {
            UnsafeNative.SetWindowExTransparent(windowHandleSupplier.Value);
        }

        protected void MakeLayered()
        {
            UnsafeNative.SetWindowExLayered(windowHandleSupplier.Value);
        }
    }
}