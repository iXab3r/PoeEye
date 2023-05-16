using System;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Modularity;
using ReactiveUI;

namespace PoeShared.Native;

public sealed class WindowViewController : DisposableReactiveObject, IWindowViewController
{
    public WindowViewController(Window owner)
    {
        Window = owner;
        Handle = new WindowInteropHelper(owner).EnsureHandle();
        Log = typeof(WindowViewController).PrepareLogger().WithSuffix(() => $"WVC for {Handle.ToHexadecimal()}");
        Log.Debug(() => $"Binding ViewController to window, {new {owner.IsLoaded, owner.RenderSize, owner.Title, owner.WindowState, owner.ShowInTaskbar}}");

        WhenRendered = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => owner.ContentRendered += h, h => owner.ContentRendered -= h)
            .ToUnit();
        WhenUnloaded = Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Unloaded += h, h => owner.Unloaded -= h)
            .ToUnit();
        WhenLoaded = Observable.Merge(
                Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Loaded += h, h => owner.Loaded -= h).ToUnit(),
                Observable.Return(Unit.Default).Where(x => owner.IsLoaded).ToUnit())
            .Take(1);
            
        WhenClosing = Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(h => owner.Closing += h, h => owner.Closing -= h).Select(x => x.EventArgs);
        WhenClosed = Observable.FromEventPattern<EventHandler, EventArgs>(h => owner.Closed += h, h => owner.Closed -= h).ToUnit();
        WhenDeactivated = Observable.FromEventPattern<EventHandler, EventArgs>(h => owner.Deactivated += h, h => owner.Deactivated -= h).ToUnit();

        this.WhenAnyValue(x => x.Topmost)
            .ObserveOn(Window.Dispatcher)
            .SubscribeSafe(x => owner.Topmost = x, Log.HandleUiException)
            .AddTo(Anchors);

        WhenDeactivated
            .Where(_ => Topmost)
            .ObserveOn(Window.Dispatcher)
            .SubscribeSafe(() =>
            {
                Log.Debug(() => $"Window is deactivated, reactivating {nameof(Topmost)} style");
                owner.Topmost = false;
                owner.Topmost = true;
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
 
    public IObservable<Unit> WhenDeactivated { get; }
        
    public IObservable<CancelEventArgs> WhenClosing { get; }

    public IObservable<Unit> WhenRendered { get; }
        
    public IntPtr Handle { get; }
        
    public Window Window { get; }

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
        Log.Debug(() => $"Closing window, result: {result}");
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
        Log.Debug(() => $"Hiding window");
        UnsafeNative.HideWindow(Window);
    }

    public void Show()
    {
        Log.Debug(() => $"Showing window");
        UnsafeNative.ShowWindow(Window);
        //FIXME Mahapps window resets topmost after minimize/maximize operations
        Window.Topmost = Topmost;
    }

    public void TakeScreenshot(string fileName)
    {
        CreateBitmapFromVisual(Window?.Content as Visual ?? Window, fileName);
    }

    public void Minimize()
    {
        Log.Debug(() => $"Minimizing window");
        Window.WindowState = WindowState.Minimized;
    }
        
    public static void CreateBitmapFromVisual(Visual target, string fileName)
    {
        if (target == null || string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var bounds = target is FrameworkElement frameworkElement ? new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight) : VisualTreeHelper.GetDescendantBounds(target);
        var dpi = VisualTreeHelper.GetDpi(target);
        var renderTarget = new RenderTargetBitmap(
            (int)(bounds.Width / 96d * dpi.PixelsPerInchX), 
            (int)(bounds.Height / 96d * dpi.PixelsPerInchY), 
            dpi.PixelsPerInchX, 
            dpi.PixelsPerInchY, 
            PixelFormats.Pbgra32);
        renderTarget.Render(target);

        var bitmapEncoder = new PngBitmapEncoder();
        bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using (Stream stm = File.Create(fileName))
        {
            bitmapEncoder.Save(stm);
        }
    }
}