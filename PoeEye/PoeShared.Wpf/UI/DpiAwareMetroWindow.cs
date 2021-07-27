using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PInvoke;

namespace PoeShared.UI
{
    public class DpiAwareMetroWindow : MetroWindow
    {
        public static readonly DependencyProperty DpiProperty = DependencyProperty.Register(
            "Dpi", typeof(DpiScale), typeof(MetroWindow), new PropertyMetadata(default(DpiScale)));
        
        private const int HWND_MESSAGE = -3;
        
        public DpiAwareMetroWindow()
        {
            this.LocationChanged += OnLocationChanged;
        }

        public DpiScale Dpi
        {
            get { return (DpiScale) GetValue(DpiProperty); }
            set { SetValue(DpiProperty, value); }
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            Dpi = VisualTreeHelper.GetDpi(this);
        }
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                User32.SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
            }
        }
    }
}