using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PoeShared.Native;

namespace PoeShared.UI;

public class DpiAwareMetroWindow : ReactiveMetroWindow
{
    public static readonly DependencyProperty DpiProperty = DependencyProperty.Register(
        nameof(Dpi), typeof(DpiScale), typeof(MetroWindow), new PropertyMetadata(default(DpiScale)));

    public DpiAwareMetroWindow()
    {
        LocationChanged += OnLocationChanged;
    }

    public DpiScale Dpi
    {
        get => (DpiScale) GetValue(DpiProperty);
        set => SetValue(DpiProperty, value);
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
            UnsafeNative.SetParentToMessageOnly(hwndSource.Handle);
        }
    }
}