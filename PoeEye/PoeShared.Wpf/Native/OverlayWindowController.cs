using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using DynamicData;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Native;

public class OverlayWindowController : DisposableReactiveObject, IOverlayWindowController
{
    private static long globalIdx;
    private readonly string overlayControllerId = $"OC#{Interlocked.Increment(ref globalIdx)}";

    private readonly IScheduler overlayScheduler;

    private readonly ISourceList<OverlayWindowView> windows = new SourceList<OverlayWindowView>();

    public OverlayWindowController(IScheduler overlayScheduler)
    {
        Guard.ArgumentNotNull(overlayScheduler, nameof(overlayScheduler));

        Log = GetType().PrepareLogger().WithSuffix(overlayControllerId);

        this.overlayScheduler = overlayScheduler;

        Children = windows
            .Connect()
            .Transform(x => ((OverlayWindowContainer)x.DataContext).Content)
            .AsObservableList();

        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    protected IFluentLog Log { get; }

    public bool ShowWireframes { get; set; }

    public bool IsVisible { get; private set; } = true;

    public IDisposable RegisterChild(IOverlayViewModel viewModel)
    {
        return RegisterChild<OverlayWindowView>(viewModel);
    }
    
    public IDisposable RegisterChild<TWindow>(IOverlayViewModel viewModel) where TWindow : OverlayWindowView, new()
    {
        if (!overlayScheduler.IsOnScheduler())
        {
            Log.Debug($"Invoking registration on {overlayScheduler}");
            return overlayScheduler.Invoke(() => RegisterChild(viewModel));
        }

        Log.Debug("Registering new child");
        var logger = Log.WithSuffix(viewModel);

        var childAnchors = new CompositeDisposable();
        Disposable.Create(() =>
        {
            logger.Debug($"Disposing overlay view model: {viewModel}");
            viewModel.Dispose();
            logger.Debug($"Disposed overlay view model: {viewModel}");
        }).AddTo(childAnchors);

        logger.Debug("Initializing window container view model");
        var windowContainer = new OverlayWindowContainer(logger)
        {
            Content = viewModel
        }.AddTo(childAnchors);
        
        logger.Debug($"Initialized window container: {windowContainer}");
        var window = new TWindow()
        {
            Visibility = Visibility.Collapsed,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
        };
        logger.AddSuffix(() => window);
        
        logger.Info($"Created overlay window");
        window.DataContext = windowContainer;
        logger.Debug($"Assigned data context");

        var activationController = new ActivationController(window);
        viewModel.SetActivationController(activationController);

        this.WhenAnyValue(x => x.ShowWireframes)
            .ObserveOn(overlayScheduler)
            .SubscribeSafe(() => windowContainer.ShowWireframes = ShowWireframes, Log.HandleUiException)
            .AddTo(childAnchors);

        window.WhenLoaded()
            .Do(_ => logger.Debug($"Overlay is loaded"))
            .SubscribeSafe(() =>
            {
                logger.Debug($"Assigning overlay view {window} to view-model {viewModel}");
                viewModel.SetOverlayWindow(window.Controller);
            }, logger.HandleUiException)
            .AddTo(childAnchors);
            
        Observable.Merge(
                this.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Controller IsVisible change, {x.prev} => {x.curr}"), 
                viewModel.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Overlay IsVisible change, {x.prev} => {x.curr}"), 
                window.WhenLoaded().Select(_ => $"[IsVisible {IsVisible}] Processing WhenLoaded event"))
            .Sample(UiConstants.UiThrottlingDelay)
            .ObserveOn(overlayScheduler)
            .SubscribeSafe(reason =>
            {
                logger.Debug($"Processing visibility change, reason: {reason}");
                HandleVisibilityChange(logger, window, viewModel);
            }, logger.HandleUiException)
            .AddTo(childAnchors);

        window.WhenLoaded()
            .Select(_ => viewModel.WhenAnyValue(x => x.OverlayMode))
            .Switch()
            .ObserveOn(overlayScheduler)
            .SubscribeSafe(x =>
            {
                logger.Debug($"Changing overlay mode to {x}");
                window.SetOverlayMode(x);
            }, logger.HandleUiException)
            .AddTo(childAnchors);
        
        window.WhenLoaded()
            .Select(_ => viewModel.WhenAnyValue(x => x.IsFocusable))
            .Switch()
            .ObserveOn(overlayScheduler)
            .SubscribeSafe(x =>
            {
                logger.Debug($"Changing isFocusable to {x}");
                window.SetActivation(x);
            }, logger.HandleUiException)
            .AddTo(childAnchors);

        window
            .WhenRendered
            .Take(1)
            .Do(_ => { logger.Debug($"Overlay is rendered"); })
            .SubscribeToErrors(logger.HandleUiException)
            .AddTo(childAnchors);

        windows.Add(window);
            
        Disposable.Create(() =>
        {
            logger.Debug($"Removing overlay, overlayList: {windows.Items.Select(x => x.Name).ToArray()}");
            windows.Remove(window);
        }).AddTo(childAnchors);
        
        Disposable.Create(() =>
        {
            logger.Info($"Closing overlay");
            window.Close();
        }).AddTo(childAnchors);

        childAnchors.AddTo(viewModel.Anchors);
        childAnchors.AddTo(Anchors);

        Log.Info($"Overlay view initialized: {window}");
        logger.Debug($"Registration completed");

        return childAnchors;
    }

    public IObservableList<IOverlayViewModel> Children { get; } 

    private void HandleVisibilityChange(IFluentLog logger, OverlayWindowView overlayWindow, IOverlayViewModel viewModel)
    {
        if (IsVisible && viewModel.IsVisible)
        {
            if (overlayWindow.Visibility != Visibility.Visible)
            {
                logger.Debug($"Overlay visibility is {overlayWindow.Visibility}, setting to Visible");
                overlayWindow.Visibility = Visibility.Visible;
            }

            var currentRect = UnsafeNative.GetWindowRect(overlayWindow.WindowHandle);
            logger.Debug($"Showing overlay {overlayWindow}, native bounds: {currentRect}");
            UnsafeNative.ShowInactiveTopmost(overlayWindow.WindowHandle);
        } else if (overlayWindow.WindowHandle == IntPtr.Zero)
        {
            logger.Debug($"Overlay is not initialized yet");
        }
        else
        {
            logger.Debug($"Hiding overlay");

            UnsafeNative.HideWindow(overlayWindow.WindowHandle);
        }
    }

    protected void SetVisibility(bool isVisible)
    {
        if (isVisible == IsVisible)
        {
            return;
        }

        Log.Debug($"Overlay controller IsVisible = {IsVisible} => {isVisible}");
        IsVisible = isVisible;
    }

    private class ActivationController : IActivationController
    {
        private readonly OverlayWindowView overlayWindow;

        public ActivationController(OverlayWindowView overlayWindow)
        {
            this.overlayWindow = overlayWindow;
        }

        public void Activate()
        {
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
            UnsafeNative.SetForegroundWindow(overlayWindowHandle);
        }
    }
}