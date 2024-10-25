using System;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
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

    public async Task<T> ShowDialog<T>(Func<T> contentFactory, CancellationToken cancellationToken = default) where T : IWindowViewModel
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
        var content = await Show(contentFactory, cancellationToken);
        var log = Log.WithSuffix(content);

        log.Info($"Awaiting for window to close: {content}");

        var closeReason = await
            Observable.Merge(
                    content.WindowController.WhenClosed.Select(x => "window closed"),
                    content.WindowController.ListenWhenDisposed().Select(x => "content disposed"))
                .Take(1)
                .Do(x => { });
        log.Info($"Window has been closed, reason: {closeReason}, content: {content}");
        return content;
    }

    public async Task<T> Show<T>(Func<T> contentFactory, CancellationToken cancellationToken = default) where T : IWindowViewModel
    {
        Log.Info($"Showing new window with content of type {typeof(T)}");

        var dispatcherId = $"UI-{Interlocked.Increment(ref globalIdx)}";
        var dispatcher = schedulerProvider.CreateDispatcherScheduler(dispatcherId, ThreadPriority.Normal);

        var windowCompletionSource = new TaskCompletionSource<T>();

        dispatcher.Schedule(() =>
        {
            Log.Info($"Creating new window for data of type {typeof(T)}");

            var window = new MetroChildWindow().AddTo(Anchors); // minor memory leak
            var log = Log.WithSuffix(window);
            Log.Info($"Created new window");

            Log.Info($"Creating new data context for window of type {typeof(T)}");
            var content = contentFactory().AddTo(window.Anchors);
            log.AddSuffix(content);

            if (content.Anchors.IsDisposed)
            {
                windowCompletionSource.SetException(new InvalidOperationException($"Window Content is already disposed before window is even created: {content}"));
                return;
            }

            Disposable.Create(() =>
            {
                log.Info($"Window content is being disposed");
                if (!window.Anchors.IsDisposed)
                {
                    log.Info($"Window content is disposed - disposing the window itself");
                    window.Dispose();
                }
            }).AddTo(content.Anchors);

            Disposable.Create(() =>
            {
                log.Info($"Shutting down dispatcher for {dispatcherId}");
                dispatcher.Dispatcher.InvokeShutdown();
                log.Info($"Dispatcher disposed: {dispatcherId}");
            }).AddTo(window.Anchors);

            var mainWindowHandle = applicationAccessor.MainWindow.GetWindowHandle();
            if (mainWindowHandle != IntPtr.Zero)
            {
                window.ListenWhenLoaded()
                    .Subscribe(() => { User32.SetWindowLong(window.WindowHandle, User32.WindowLongIndexFlags.GWLP_HWNDPARENT, (User32.SetWindowLongFlags) mainWindowHandle); })
                    .AddTo(content.Anchors);
            }

            window.DataContext = content;
            var ownerWindowRect = UnsafeNative.GetWindowRect(mainWindowHandle);
            var childSize = content.DefaultSize.IsNotEmptyArea() ? content.DefaultSize : content.MinSize;
            var updatedBounds = childSize.CenterInsideBounds(ownerWindowRect);
            log.Debug($"Centering rect {childSize} inside parent {ownerWindowRect}, result: {updatedBounds}");
            content.NativeBounds = updatedBounds;

            window.ListenWhenLoaded()
                .Subscribe(() =>
                {
                    log.Info($"Window has been loaded");
                    content.SetOverlayWindow(window.Controller);
                    windowCompletionSource.SetResult(content);
                })
                .AddTo(content.Anchors);

            window.ListenWhenDisposed()
                .Subscribe(() =>
                {
                    log.Info($"Window has been disposed");
                    if (windowCompletionSource.TrySetCanceled())
                    {
                        log.Info($"Window creation has been cancelled");
                    }
                })
                .AddTo(content.Anchors);

            try
            {
                log.Info($"Showing the window");
                var closeController = new WindowCloseController(log, window);
                if (content is ICloseable closeable)
                {
                    closeable.CloseController = closeController;
                }

                if (cancellationToken != default)
                {
                    window.WhenLoaded()
                        .Subscribe(x =>
                        {
                            cancellationToken.Register(() =>
                            {
                                Log.Debug($"Got Close signal via cancellation token, closing {window}");
                                closeController.Close();
                            }).AddTo(window.Anchors);
                        })
                        .AddTo(content.Anchors);
                }

                window.ShowDialog();
            }
            catch (Exception e)
            {
                log.Error("Window has closed with an error", e);
                throw;
            }
        }).AddTo(Anchors);

        Log.Debug($"Awaiting for window to be loaded");
        var result = await windowCompletionSource.Task;
        Log.Debug($"Window has loaded: {result}");
        return result;
    }

    public Task<T> ShowWindow<T>(Func<T> windowFactory) where T : Window
    {
        Log.Debug($"Showing new window with content of type {typeof(T)}");

        var dispatcherId = $"UI-{Interlocked.Increment(ref globalIdx)}";
        var dispatcher = schedulerProvider.CreateDispatcherScheduler(dispatcherId, ThreadPriority.Normal);

        var windowCompletionSource = new TaskCompletionSource<T>();
        var isLoaded = new ManualResetEventSlim(false);
        dispatcher.Schedule(() =>
        {
            Log.Debug($"Creating new window for data of type {typeof(T)}");
            var window = windowFactory();
            Log.Debug($"Created new window: {window}");

            window.Loaded += (sender, args) =>
            {
                Log.Debug($"Window has loaded: {window}");
                isLoaded.Set();
            };

            windowCompletionSource.SetResult(window);

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
        isLoaded.Wait();
        Log.Debug($"Window has loaded");
        return windowCompletionSource.Task;
    }

    private sealed class WindowCloseController : ICloseController
    {
        public WindowCloseController(IFluentLog log, ReactiveMetroWindow window)
        {
            Log = log;
            Window = window;
        }
        
        public IFluentLog Log { get; }
        
        public ReactiveMetroWindow Window { get; }

        public void Close()
        {
            Window.Dispose();
        }
    }
}