using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Behaviors;
using Microsoft.Xaml.Behaviors;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public partial class OverlayWindowView
{
    public OverlayWindowView()
    {
        InitializeComponent();
        BorderThickness = new Thickness(0);
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = sender as Window;
        var windowViewModel = window?.DataContext as WindowContainerBase<IOverlayViewModel>;
        var overlayViewModel = windowViewModel?.Content;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        Log.Debug($"Window size changed");

        var window = sender as Window;
        var windowViewModel = window?.DataContext as WindowContainerBase<IOverlayViewModel>;
        var overlayViewModel = windowViewModel?.Content;
        if (overlayViewModel == null)
        {
            return;
        }

        var delta = sizeChangedEventArgs.NewSize.Height - sizeChangedEventArgs.PreviousSize.Height;
        if (!overlayViewModel.GrowUpwards)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() => Top -= delta), DispatcherPriority.Render);
    }
}