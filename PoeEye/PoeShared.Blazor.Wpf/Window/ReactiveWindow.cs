using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Interop;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf;

public class ReactiveWindow : Window, IDisposableReactiveObject
{
    public CompositeDisposable Anchors { get; } = new();

    public IntPtr WindowHandle { get; private set; }

    private bool AllowsTransparencyAfterLoad { get; set; }

    public ReactiveWindow()
    {
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        WindowHandle = new WindowInteropHelper(this).EnsureHandle(); // should be already available here
        if (WindowHandle == IntPtr.Zero)
        {
            throw new InvalidStateException("Window handle must be initialized at this point");
        }

        base.OnSourceInitialized(e);
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AllowsTransparencyAfterLoad = AllowsTransparency;
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        RaisePropertyChanged(e.Property.Name);
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Dispose();
    }

    /// <summary>
    ///     https://stackoverflow.com/questions/17297539/can-ui-automation-be-disabled-for-an-entire-wpf-4-0-app
    ///     https://stackoverflow.com/questions/6362367/wpf-ui-automation-issue
    ///     https://stackoverflow.com/questions/5716078/wpf-performance-issue-due-to-ui-automation
    /// </summary>
    /// <returns></returns>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new NoopWindowAutomationPeer(this);
    }


    protected void MakeTransparent()
    {
        UnsafeNative.SetWindowExTransparent(WindowHandle);
    }

    protected void MakeLayered()
    {
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
            throw new InvalidOperationException("Transparent mode requires AllowsTransparency to be set to True");
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