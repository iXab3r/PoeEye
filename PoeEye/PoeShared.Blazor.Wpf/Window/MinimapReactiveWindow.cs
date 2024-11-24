using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Future host for Blazor windows - stripped down window that should eventually use winforms as they are faster
/// </summary>
internal class MinimapReactiveWindow : Window, IDisposable, INotifyPropertyChanged
{
    private static long GlobalWindowId;

    public MinimapReactiveWindow()
    {
        Log = GetType().PrepareLogger()
            .WithSuffix(WindowId)
            .WithSuffix(() => DataContext == default ? "Data context is not set" : DataContext.ToString());

        Loaded += OnLoaded;
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public CompositeDisposable Anchors { get; } = new CompositeDisposable();
    public string WindowId { get; } = $"BWnd#{Interlocked.Increment(ref GlobalWindowId)}";

    public IntPtr WindowHandle { get; private set; }

    public bool ShowTitleBar { get; set; }
    public bool ShowSystemMenu { get; set; }
    public bool ShowSystemMenuOnRightClick { get; set; }
    public bool ShowMinButton { get; set; }
    public bool ShowMaxRestoreButton { get; set; }
    public bool ShowCloseButton { get; set; }

    protected IFluentLog Log { get; }

    private bool AllowsTransparencyAfterLoad { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AllowsTransparencyAfterLoad = AllowsTransparency;
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

    protected void MakeTransparent()
    {
        UnsafeNative.SetWindowExTransparent(WindowHandle);
    }

    protected void MakeLayered()
    {
        UnsafeNative.SetWindowExLayered(WindowHandle);
    }
}