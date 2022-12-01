using System;
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
        ShowTitleBar = false;
        ShowSystemMenu = false;
        ShowCloseButton = false;
        ShowMinButton = false;
        ShowMaxRestoreButton = false;
        ShowSystemMenuOnRightClick = false;
        SizeChanged += OnSizeChanged;
        this.WhenLoaded().SubscribeSafe(OnLoaded, Log.HandleUiException).AddTo(Anchors);
    }
    
    private bool AllowsTransparencyAfterLoad { get; set; }

    private void OnLoaded()
    {
        Log.Debug(() => $"Setting WindowExNoActivate");
        AllowsTransparencyAfterLoad = AllowsTransparency;
        UnsafeNative.SetWindowExNoActivate(WindowHandle);
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