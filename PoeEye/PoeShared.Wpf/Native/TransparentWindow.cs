using System;
using System.Windows;

namespace PoeShared.Native;

public class TransparentWindow : ConstantAspectRatioWindow
{
    public TransparentWindow()
    {
        Loaded += OnLoaded;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        Log.Debug(() => "Created window");
    }

    private void OnDeactivated(object sender, EventArgs e)
    {
        Log.Debug(() => "Window is deactivated");
    }

    private void OnActivated(object sender, EventArgs e)
    {
        Log.Debug(() => "Window is activated");
    }

    private bool AllowsTransparencyAfterLoad { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug(() => "Window is loaded - hiding system menu");
        UnsafeNative.HideSystemMenu(WindowHandle);
        Log.Debug(() => $"Setting WindowExNoActivate");
        AllowsTransparencyAfterLoad = AllowsTransparency;
    }

    protected void MakeTransparent()
    {
        Log.Debug(() => "Making window transparent");
        UnsafeNative.SetWindowExTransparent(WindowHandle);
    }

    protected void MakeLayered()
    {
        Log.Debug(() => "Making window layered");
        UnsafeNative.SetWindowExLayered(WindowHandle);
    }

    public void SetActivation(bool isFocusable)
    {
        if (isFocusable)
        {
            UnsafeNative.SetWindowExActivate(WindowHandle);
        }
        else
        {
            UnsafeNative.SetWindowExNoActivate(WindowHandle);
        }
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