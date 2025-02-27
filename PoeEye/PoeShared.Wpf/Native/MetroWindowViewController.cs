using System;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Native;

public sealed class MetroWindowViewController : DisposableReactiveObject, IMetroWindowViewController
{
    private readonly Lazy<IntPtr> windowHandle;

    public MetroWindowViewController(ReactiveMetroWindow owner)
    {
        this.Window = owner;
        windowHandle = new Lazy<IntPtr>(() => new WindowInteropHelper(owner).EnsureHandle());
        Log = typeof(MetroWindowViewController).PrepareLogger().WithSuffix(() => $"WVC");
        Log.Debug($"Binding ViewController to window, {new {owner.IsLoaded, owner.RenderSize, owner.Title, owner.WindowState, owner.ShowInTaskbar}}");

        WhenLoaded = owner.ListenWhenLoaded();
        WhenRendered = owner.ListenWhenRendered();
        WhenUnloaded = owner.ListenWhenUnloaded();

        WhenKeyUp = owner.ListenWhenKeyUp();
        WhenKeyDown = owner.ListenWhenKeyDown();
        WhenPreviewKeyDown = owner.ListenWhenPreviewKeyDown();
        WhenPreviewKeyUp = owner.ListenWhenPreviewKeyUp();
        WhenClosing = owner.ListenWhenClosing();
        WhenClosed = owner.ListenWhenClosed();
        WhenActivated = owner.ListenWhenActivated();
        WhenDeactivated = owner.ListenWhenDeactivated();

        this.WhenAnyValue(x => x.Topmost)
            .ObserveOn(Window.Dispatcher)
            .SubscribeSafe(x => owner.Topmost = x, Log.HandleUiException)
            .AddTo(Anchors);

        WhenDeactivated
            .Where(_ => Topmost)
            .ObserveOn(Window.Dispatcher)
            .SubscribeSafe(() =>
            {
                Log.Debug($"Window is deactivated, reactivating {nameof(Topmost)} style");
                UnsafeNative.MakeTopmost(owner.WindowHandle);
            }, Log.HandleUiException)
            .AddTo(Anchors);

        new ScheduledDisposable(DispatcherScheduler.Current, Disposable.Create(() =>
        {
            if (Window.IsVisible)
            {
                Log.Debug("Controlled is being disposed, closing the window");
                Close();   
            }
        })).AddTo(Anchors);
    }
        
    private IFluentLog Log { get; }

    public IObservable<Unit> WhenLoaded { get; }
        
    public IObservable<Unit> WhenUnloaded { get; }
        
    public IObservable<Unit> WhenClosed { get; }
 
    public IObservable<Unit> WhenActivated { get; }
    
    public IObservable<Unit> WhenDeactivated { get; }
        
    public IObservable<CancelEventArgs> WhenClosing { get; }

    public IObservable<Unit> WhenRendered { get; }
    
    public IObservable<KeyEventArgs> WhenKeyUp { get; }
    
    public IObservable<KeyEventArgs> WhenKeyDown { get; }
    
    public IObservable<KeyEventArgs> WhenPreviewKeyDown { get; }
    
    public IObservable<KeyEventArgs> WhenPreviewKeyUp { get; }

    public IntPtr Handle => windowHandle.Value;

    public ReactiveMetroWindow Window { get; }

    public void Activate()
    {
        Log.Debug("Activating window");
        if (!Window.Dispatcher.CheckAccess())
        {
            Log.Debug("Rescheduling to window dispatcher");
            Window.Dispatcher.Invoke(Activate);
            return;
        }
        Window.Activate();
        Log.Debug("Activated window");
    }

    public void Close(bool? result)
    {
        Log.Debug($"Closing window, result: {result}");
        if (!Window.Dispatcher.CheckAccess())
        {
            Log.Debug("Rescheduling to window dispatcher");
            Window.Dispatcher.Invoke(() => Close(result));
            return;
        }
        
        Window.DialogResult = result;
        Window.Close();
        Log.Debug("Closed window");
    }

    public void Close()
    {
        Close(null);
    }

    public bool Topmost { get; set; }

    public void Hide()
    {
        Log.Debug($"Hiding window");
        UnsafeNative.HideWindow(Window);
    }

    public void Show()
    {
        Log.Debug($"Showing window");
        UnsafeNative.ShowWindow(Window);
        //FIXME Mahapps window resets topmost after minimize/maximize operations
        Window.Topmost = Topmost;
        if (Topmost)
        {
            UnsafeNative.MakeTopmost(windowHandle.Value);
        }
    }

    public void TakeScreenshot(string fileName)
    {
        var visual = Window?.Content as Visual ?? Window;
        visual?.CreateBitmapFromVisual(fileName);
    }

    public void Minimize()
    {
        Log.Debug($"Minimizing window");
        Window.WindowState = WindowState.Minimized;
    }
}