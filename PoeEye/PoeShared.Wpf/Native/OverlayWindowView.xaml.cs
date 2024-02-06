using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Behaviors;
using Microsoft.Xaml.Behaviors;
using PoeShared.Scaffolding;
using ReactiveUI;

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

        this.Observe(DataContextProperty, x => x.DataContext).OfType<OverlayWindowContainer>()
            .CombineLatest(this.WhenAnyValue(x => x.NativeBounds), (container, bounds) => (container, bounds))
            .Where(x => x.container != null)
            .Subscribe(x => x.container.NativeBounds = x.bounds)
            .AddTo(Anchors);
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