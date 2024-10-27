using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Interop;
using System.Windows.Media;
using ControlzEx.Behaviors;
using DynamicData;
using MahApps.Metro.Behaviors;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class ReactiveMetroWindowBase : MetroWindow, IDisposableReactiveObject
{
    private static long GlobalWindowId;

    public ReactiveMetroWindowBase()
    {
        Log = GetType().PrepareLogger()
            .WithSuffix(WindowId)
            .WithSuffix(() => NativeWindowId)
            .WithSuffix(() => DataContext == default ? "Data context is not set" : DataContext.ToString());
       
        PreferDWMBorderColor = false;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        
        Closed += OnClosed;
        Loaded += OnLoaded;
        Initialized += OnInitialized;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    protected IFluentLog Log { get; }

    public string WindowId { get; } = $"Wnd#{Interlocked.Increment(ref GlobalWindowId)}";

    public IScheduler Scheduler => DispatcherScheduler.Current;

    private bool AllowsTransparencyAfterLoad { get; set; }

    public IntPtr WindowHandle { get; private set; }

    public string NativeWindowId =>
        WindowHandle == IntPtr.Zero ? "Native window not created yet" : WindowHandle.ToHexadecimal();

    public CompositeDisposable Anchors { get; } = new();

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnDeactivated(object sender, EventArgs e)
    {
        Log.Debug("Window is deactivated");
    }

    private void OnInitialized(object sender, EventArgs e)
    {
        Log.Debug("Window initialized");
        Log.Debug("Initializing native window handle");
       // new WindowInteropHelper(this).EnsureHandle(); //EnsureHandle leads to SourceInitialized
        Log.Debug("Native window initialized");
    }

    private void OnActivated(object sender, EventArgs e)
    {
        Log.Debug("Window is activated");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("Setting WindowExNoActivate");
        AllowsTransparencyAfterLoad = AllowsTransparency;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowHandle = new WindowInteropHelper(this).EnsureHandle(); // should be already available here
        if (WindowHandle == IntPtr.Zero)
        {
            throw new InvalidStateException("Window handle must be initialized at this point");
        }

        var behaviors = Interaction.GetBehaviors(this);
        Log.Debug($"Default behaviors: {behaviors.DumpToString()}");

        var behaviorTypeToRemove = new[]
        {
            typeof(GlowWindowBehavior), 
        };
        var behaviorToRemove = behaviors.Where(x => behaviorTypeToRemove.Contains(x.GetType())).ToArray();
        behaviors.RemoveMany(behaviorToRemove);
        behaviors.Add(new RestoreWindowWhenActivatedXamlBehavior());
        Log.Debug($"Removing the following behaviors: {behaviorToRemove.DumpToString()}");
    }

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
        Log.Debug("Making window transparent");
        UnsafeNative.SetWindowExTransparent(WindowHandle);
    }

    protected void MakeLayered()
    {
        Log.Debug("Making window layered");
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