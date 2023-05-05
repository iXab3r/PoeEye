﻿using System;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public partial class OverlayWindowView
{
    public OverlayWindowView()
    {
        InitializeComponent();
        BorderThickness = new Thickness(0);
        SizeChanged += OnSizeChanged;
    }
    
    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        Log.Debug(() => $"Window size changed");

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