using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public partial class OverlayWindowView
{
    public OverlayWindowView()
    {
        using var sw = new BenchmarkTimer("View initialization", Log, nameof(OverlayWindowView));
        InitializeComponent();
        sw.Step("Components initialized");
        WhenLoaded.SubscribeSafe(OnLoaded, Log.HandleUiException).AddTo(Anchors);
        sw.Step("WhenLoaded routine executed");
        SizeChanged += OnSizeChanged;
        LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object sender, EventArgs e)
    {
        Log.Debug(() => $"Window location changed");
    }

    public IObservable<EventPattern<RoutedEventArgs>> WhenLoaded => Observable
        .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => Loaded += h, h => Loaded -= h);

    public IObservable<EventPattern<EventArgs>> WhenRendered => Observable
        .FromEventPattern<EventHandler, EventArgs>(h => ContentRendered += h, h => ContentRendered -= h);

    private bool AllowsTransparencyAfterLoad { get; set; }

    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        Log.Debug(() => $"Window size changed");

        var window = sender as Window;
        var windowViewModel = window?.DataContext as OverlayWindowViewModel;
        var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
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

    private void OnLoaded()
    {
        Log.Debug(() => $"Setting WindowExNoActivate");
        AllowsTransparencyAfterLoad = AllowsTransparency;
        UnsafeNative.SetWindowExNoActivate(WindowHandle);
    }

    public override string ToString()
    {
        return $"{base.ToString()} DataContext: {DataContext.Dump()} {{X={Left:F0},Y={Top:F0},Width={Width:F0},Height={Height:F0}}}";
    }

    public void SetOverlayMode(OverlayMode mode)
    {
        if (AllowsTransparencyAfterLoad == false && mode == OverlayMode.Transparent)
        {
            throw new InvalidOperationException($"Transparent mode requires AllowsTransparency to be set to True");
        }

        switch (mode)
        {
            case OverlayMode.Layered:
                MakeLayered();
                break;
            case OverlayMode.Transparent:
                MakeTransparent();
                break;
        }
    }
}