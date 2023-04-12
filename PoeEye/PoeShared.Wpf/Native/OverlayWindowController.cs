using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using DynamicData;

using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Native;

internal sealed class OverlayWindowController : DisposableReactiveObject, IOverlayWindowController
{
    private static long GlobalIdx;
    private readonly ReadOnlyObservableCollection<IntPtr> childWindows;
    private readonly string overlayControllerId = $"OC#{Interlocked.Increment(ref GlobalIdx)}";

    private readonly IScheduler uiScheduler;

    private readonly ISourceList<OverlayWindowView> windows = new SourceListEx<OverlayWindowView>();
    private readonly IWindowTracker windowTracker;

    public OverlayWindowController(
        [NotNull] IWindowTracker windowTracker,
        [NotNull] [Unity.Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        Guard.ArgumentNotNull(windowTracker, nameof(windowTracker));
        Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

        Log = GetType().PrepareLogger().WithSuffix(overlayControllerId);
        Log.Info(() => $"Creating overlay window controller using {windowTracker}");

        this.windowTracker = windowTracker;
        this.uiScheduler = uiScheduler;

        windows
            .Connect()
            .ObserveOn(uiScheduler)
            .Transform(x => new WindowInteropHelper(x).EnsureHandle())
            .Bind(out childWindows)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);

        Observable.Merge(windowTracker.WhenAnyValue(x => x.ActiveWindowHandle).ToUnit(), this.WhenAnyValue(x => x.IsEnabled).ToUnit())
            .Select(
                x => new
                {
                    ActiveWindowHandle = windowTracker.ActiveWindowHandle,
                    ActiveTitle = windowTracker.ActiveWindowTitle,
                    WindowIsActive = windowTracker.IsActive,
                    OverlayIsActive = IsPairedOverlay(windowTracker.ActiveWindowHandle),
                    IsEnabled
                })
            .Do(x => Log.Debug(() => $"Active window has changed: {x}"))
            .Select(x => (x.WindowIsActive || x.OverlayIsActive) && IsEnabled)
            .DistinctUntilChanged()
            .Do(x => Log.Debug(() => $"Sending SetVisibility({x}) to window scheduler"))
            .ObserveOn(uiScheduler)
            .SubscribeSafe(SetVisibility, Log.HandleUiException)
            .AddTo(Anchors);

        windowTracker
            .WhenAnyValue(x => x.MatchingWindow)
            .Where(x => x != null && !IsPairedOverlay(windowTracker.ActiveWindowHandle))
            .SubscribeSafe(x => LastActiveWindow = x, Log.HandleUiException)
            .AddTo(Anchors);

        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    private IFluentLog Log { get; }

    public bool ShowWireframes { get; set; }

    public bool IsVisible { get; private set; }

    public bool IsEnabled { get; set; } = true;
        
    public IWindowHandle LastActiveWindow { get; private set; }

    public IOverlayViewModel[] GetChildren()
    {
        return windows.Items
            .Select(x => x.DataContext)
            .OfType<OverlayWindowContainer>()
            .Select(x => x.Content)
            .ToArray();
    }

    public IDisposable RegisterChild(IOverlayViewModel viewModel)
    {
        Guard.ArgumentNotNull(viewModel, nameof(viewModel));
        OverlayWindowView window = default;
        var logger = Log.WithSuffix(viewModel).WithSuffix(() => window);

        var childAnchors = new CompositeDisposable();
        Disposable.Create(() =>
        {
            logger.Debug($"Disposing overlay view model: {viewModel}");
            viewModel.Dispose();
            logger.Debug($"Disposed overlay view model: {viewModel}");
        }).AddTo(childAnchors);

        logger.Debug(() =>"Initializing window container view model");
        var windowContainer = new OverlayWindowContainer(logger)
        {
            Content = viewModel
        };
        windowContainer.AddTo(childAnchors);
        logger.Debug(() => $"Initialized window container: {windowContainer}");
        window = new OverlayWindowView
        {
            Title = $"{viewModel.Id} {overlayControllerId} {windowTracker}",
            Visibility = Visibility.Collapsed,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
        };
        logger.Info(() => $"Created overlay window");
        window.DataContext = windowContainer;
        logger.Debug(() => $"Assigned data context");

        var activationController = new ActivationController(window);
        viewModel.SetActivationController(activationController);

        this.WhenAnyValue(x => x.ShowWireframes)
            .ObserveOn(uiScheduler)
            .SubscribeSafe(() => windowContainer.ShowWireframes = ShowWireframes, Log.HandleUiException)
            .AddTo(childAnchors);

        window.WhenLoaded()
            .Do(args => logger.Debug(() => $"Overlay is loaded"))
            .SubscribeSafe(() =>
            {
                logger.Debug(() => $"Assigning overlay view {window} to view-model {viewModel}");
                viewModel.SetOverlayWindow(window);
            }, logger.HandleUiException)
            .AddTo(childAnchors);
            
        Observable.Merge(
                this.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Controller IsVisible change, {x.prev} => {x.curr}"), 
                viewModel.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Overlay IsVisible change, {x.prev} => {x.curr}"), 
                windowTracker.WhenAnyValue(x => x.ActiveWindowHandle).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing ActiveWindowHandle change, {UnsafeNative.GetWindowTitle(x.prev)} {x.prev.ToHexadecimal()} => {UnsafeNative.GetWindowTitle(x.curr)} {x.curr.ToHexadecimal()}"),
                window.WhenLoaded().Select(_ => $"[IsVisible {IsVisible}] Processing WhenLoaded event"))
            .Sample(UiConstants.UiThrottlingDelay)
            .ObserveOn(uiScheduler)
            .SubscribeSafe(reason =>
            {
                logger.Debug(() => $"Processing visibility change, reason: {reason}");
                HandleVisibilityChange(logger, window, viewModel);
            }, logger.HandleUiException)
            .AddTo(childAnchors);

        window.WhenLoaded()
            .Select(_ => viewModel.WhenAnyValue(x => x.OverlayMode))
            .Switch()
            .ObserveOn(uiScheduler)
            .SubscribeSafe(x =>
            {
                logger.Debug(() => $"Changing overlay mode to {x}");
                window.SetOverlayMode(x);
            }, logger.HandleUiException)
            .AddTo(childAnchors);
        
        window.WhenLoaded()
            .Select(_ => viewModel.WhenAnyValue(x => x.IsFocusable))
            .Switch()
            .ObserveOn(uiScheduler)
            .SubscribeSafe(x =>
            {
                logger.Debug(() => $"Changing isFocusable to {x}");
                window.SetActivation(x);
            }, logger.HandleUiException)
            .AddTo(childAnchors);

        window
            .WhenRendered
            .Take(1)
            .Do(_ => { logger.Debug(() => $"Overlay is rendered"); })
            .SubscribeToErrors(logger.HandleUiException)
            .AddTo(childAnchors);

        windows.Add(window);

        Disposable.Create(() =>
        {
            logger.Info($"Closing overlay");
            window.Close();
        }).AddTo(childAnchors);
            
        Disposable.Create(() =>
        {
            logger.Debug(() => $"Removing overlay, overlayList: {windows.Items.Select(x => x.Name).ToArray()}");
            windows.Remove(window);
        }).AddTo(childAnchors);

        childAnchors.AddTo(Anchors);

        Log.Info(() => $"Overlay view initialized: {window}");
        logger.Debug(() => $"Registration completed");

        return childAnchors;
    }

    public void ActivateLastActiveWindow()
    {
        var windowHandle = LastActiveWindow;
        if (windowHandle == default)
        {
            return;
        }

        UnsafeNative.SetForegroundWindow(windowHandle);
    }

    private void HandleVisibilityChange(IFluentLog logger, OverlayWindowView overlayWindow, IOverlayViewModel viewModel)
    {
        if (IsVisible && viewModel.IsVisible)
        {
            if (overlayWindow.Visibility != Visibility.Visible)
            {
                logger.Debug(() => $"Overlay visibility is {overlayWindow.Visibility}, setting to Visible");
                overlayWindow.Visibility = Visibility.Visible;
            }

            var currentRect = UnsafeNative.GetWindowRect(overlayWindow.WindowHandle);
            logger.Debug(() => $"Showing overlay {overlayWindow}, native bounds: {currentRect}");
            UnsafeNative.ShowInactiveTopmost(overlayWindow.WindowHandle);
        } else if (overlayWindow.WindowHandle == IntPtr.Zero)
        {
            logger.Debug(() => $"Overlay is not initialized yet");
        }
        else
        {
            logger.Debug(() => $"Hiding overlay (tracker {windowTracker})");

            UnsafeNative.HideWindow(overlayWindow.WindowHandle);
        }
    }

    private bool IsPairedOverlay(IntPtr hwnd)
    {
        return childWindows.Contains(hwnd);

    }

    private void SetVisibility(bool isVisible)
    {
        if (isVisible == IsVisible)
        {
            return;
        }

        Log.Debug(() => $"Overlay controller IsVisible = {IsVisible} => {isVisible} (tracker {windowTracker})");
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