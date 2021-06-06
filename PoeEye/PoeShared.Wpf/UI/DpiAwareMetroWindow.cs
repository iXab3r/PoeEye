using System;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace PoeShared.UI
{
    public class DpiAwareMetroWindow : MetroWindow
    {
        public static readonly DependencyProperty DpiProperty = DependencyProperty.Register(
            "Dpi", typeof(DpiScale), typeof(MetroWindow), new PropertyMetadata(default(DpiScale)));

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
    }
}