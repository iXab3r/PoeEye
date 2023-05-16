using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;

namespace PoeShared.Modularity;

internal sealed class WindowRepository : DisposableReactiveObjectWithLogger, IWindowRepository
{
    private static long globalIdx;
    
    private readonly IApplicationAccessor applicationAccessor;
    private readonly ISchedulerProvider schedulerProvider;

    public WindowRepository(
        IApplicationAccessor applicationAccessor,
        ISchedulerProvider schedulerProvider)
    {
        this.applicationAccessor = applicationAccessor;
        this.schedulerProvider = schedulerProvider;
    }
    
    public async Task ShowDialog<T>(Func<T> contentFactory) where T : IWindowViewModel
    {
        Log.Debug($"Showing new modal dialog window with content of type {typeof(T)}");

        using var windowAnchors = new CompositeDisposable();
        /* This simulates "modal" behavior of child window, but looks kinda bad (looks like window is non-responsive rather than inactive) 
        if (applicationAccessor.MainWindow != null)
        {
            var mainWindowHandle = applicationAccessor.MainWindow.GetWindowHandle();
            Log.Debug($"Disabling main window {mainWindowHandle.ToHexadecimal()}");
            if (UnsafeNative.EnableWindow(mainWindowHandle, false) == false)
            {
                Log.Debug($"Disabled main window {mainWindowHandle.ToHexadecimal()}");
                Disposable.Create(() =>
                {
                    Log.Debug($"Enabling main window {mainWindowHandle.ToHexadecimal()}");
                }).AddTo(windowAnchors);
            };
        }*/
        var controller = await Show(contentFactory);
        Log.Debug($"Awaiting for window to close: {controller}");
        await controller.WhenClosed.Take(1).Do(x => { });
        Log.Debug($"Window has closed: {controller}");
    }

    public async Task<IWindowViewController> Show<T>(Func<T> contentFactory) where T : IWindowViewModel
    {
        Log.Debug($"Showing new window with content of type {typeof(T)}");

        var dispatcherId = $"UI-{Interlocked.Increment(ref globalIdx)}";
        var dispatcher = schedulerProvider.CreateDispatcherScheduler(dispatcherId, ThreadPriority.Normal);
        
        var windowCompletionSource = new TaskCompletionSource<IWindowViewController>();
        dispatcher.Schedule(() =>
        {
            Log.Debug($"Creating new window for data of type {typeof(T)}");

            var window = new MetroChildWindow().AddTo(Anchors); // minor memory leak
            Log.Debug($"Created new window: {window}");

            Log.Debug($"Creating new data context for window of type {typeof(T)}");
            var content = contentFactory().AddTo(window.Anchors);
            //content.SetOverlayWindow(window); refactor this to allow for child window to be controlled by data context
            window.DataContext = content;
            
            Disposable.Create(() =>
            {
                Log.Info($"Shutting down dispatcher for {dispatcherId}");
                dispatcher.Dispatcher.InvokeShutdown();
                Log.Info($"Dispatcher disposed: {dispatcherId}");
            }).AddTo(window.Anchors);
            
            var mainWindowHandle = applicationAccessor.MainWindow.GetWindowHandle();
            if (mainWindowHandle != IntPtr.Zero)
            {
                window.Loaded += (sender, args) =>
                {
                    User32.SetWindowLong(window.WindowHandle, User32.WindowLongIndexFlags.GWLP_HWNDPARENT, (User32.SetWindowLongFlags) mainWindowHandle);
                };
            }

            window.Loaded += (sender, args) =>
            {
                Log.Debug($"Window has loaded: {window}");
                windowCompletionSource.SetResult(window.Controller);
            };
            
            try
            {
                Log.Debug($"Showing the window: {window}");
                window.ShowDialog();
            }
            catch (Exception e)
            {
                Log.Error("Window has closed with an error", e);
                throw;
            }
        }).AddTo(Anchors);
        
        Log.Debug($"Awaiting for window to be loaded");
        var result = await windowCompletionSource.Task;
        Log.Debug($"Window has loaded, controller: {result}");
        return result;
    }
}