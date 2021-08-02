using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Interop;
using DynamicData;

using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowController : DisposableReactiveObject, IOverlayWindowController
    {
        private static readonly IFluentLog Log = typeof(OverlayWindowController).PrepareLogger();
        private readonly BehaviorSubject<IntPtr> lastActiveWindowHandle = new BehaviorSubject<IntPtr>(IntPtr.Zero);

        private readonly IScheduler uiScheduler;

        private readonly ISourceList<OverlayWindowView> windows = new SourceList<OverlayWindowView>();
        private readonly ReadOnlyObservableCollection<IntPtr> childWindows;
        private readonly IWindowTracker windowTracker;
        private readonly string uniqueControllerId = Guid.NewGuid().ToString();

        private bool isVisible;
        private bool isEnabled;
        private bool showWireframes;

        public OverlayWindowController(
            [NotNull] IWindowTracker windowTracker,
            [NotNull] [Unity.Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(windowTracker, nameof(windowTracker));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

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
                .Do(x => Log.Debug($"Active window has changed: {x}"))
                .Select(x => (x.WindowIsActive || x.OverlayIsActive) && IsEnabled)
                .DistinctUntilChanged()
                .Do(x => Log.Debug($"Sending SetVisibility({x}) to window scheduler"))
                .ObserveOn(uiScheduler)
                .SubscribeSafe(SetVisibility, Log.HandleUiException)
                .AddTo(Anchors);

            windowTracker
                .WhenAnyValue(x => x.MatchingWindowHandle)
                .Where(x => x != IntPtr.Zero && !IsPairedOverlay(windowTracker.ActiveWindowHandle))
                .SubscribeSafe(lastActiveWindowHandle)
                .AddTo(Anchors);
        }

        public bool ShowWireframes
        {
            get => showWireframes;
            set => this.RaiseAndSetIfChanged(ref showWireframes, value);
        }

        public bool IsVisible
        {
            get => isVisible;
            private set => this.RaiseAndSetIfChanged(ref isVisible, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public IOverlayViewModel[] GetChildren()
        {
            return windows.Items
                .Select(x => x.DataContext)
                .OfType<OverlayWindowViewModel>()
                .Select(x => x.Content)
                .ToArray();
        }

        public IDisposable RegisterChild(IOverlayViewModel viewModel)
        {
            using var sw = new BenchmarkTimer($"Registering viewModel {viewModel}", Log);

            Guard.ArgumentNotNull(viewModel, nameof(viewModel));

            var childAnchors = new CompositeDisposable();

            viewModel.AddTo(childAnchors);

            var overlayWindowViewModel = new OverlayWindowViewModel
            {
                Content = viewModel
            };
            overlayWindowViewModel.AddTo(childAnchors);
            sw.Step($"Initialized {nameof(OverlayWindowViewModel)}");

            var overlayName = $"{viewModel.GetType().Name}";
            var overlayWindow = new OverlayWindowView
            {
                Title = $"[PoeEye.Overlay] {uniqueControllerId} {windowTracker} #{overlayName} #{windows.Count + 1}",
                Visibility = Visibility.Collapsed,
                ShowInTaskbar = false,
                ShowActivated = false,
                Topmost = true,
                Name = $"{overlayName}_OverlayView"
            };
            Log.Debug($"[#{overlayName}] Created Overlay window({windowTracker})");
            sw.Step($"Initialized overlay window: {overlayWindow.Title}");
            overlayWindow.DataContext = overlayWindowViewModel;
            sw.Step($"Initialized overlay data context: {overlayWindowViewModel}");

            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).EnsureHandle();
            Log.Debug($"[#{overlayName}] Overlay window({windowTracker}) handle: {overlayWindowHandle.ToHexadecimal()}");
            sw.Step($"Initialized overlay window handle: {overlayWindowHandle.ToHexadecimal()}");
            
            var activationController = new ActivationController(overlayWindow);
            viewModel.SetActivationController(activationController);

            this.WhenAnyValue(x => x.ShowWireframes)
                .ObserveOn(uiScheduler)
                .SubscribeSafe(() => overlayWindowViewModel.ShowWireframes = showWireframes, Log.HandleUiException)
                .AddTo(childAnchors);

            overlayWindow.WhenLoaded
                .Do(args => Log.Debug($"[#{overlayWindow.Name}] Overlay is loaded, window: {args.Sender}"))
                .SubscribeSafe(() => viewModel.SetOverlayWindow(overlayWindow), Log.HandleUiException)
                .AddTo(childAnchors);
            
            Observable.Merge(
                    this.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Controller IsVisible change, {x.prev} => {x.curr}"), 
                    viewModel.WhenAnyValue(x => x.IsVisible).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing Overlay IsVisible change, {x.prev} => {x.curr}"), 
                    windowTracker.WhenAnyValue(x => x.ActiveWindowHandle).WithPrevious((prev, curr) => new {prev, curr}).Select(x => $"[IsVisible {IsVisible}] Processing ActiveWindowHandle change, {UnsafeNative.GetWindowTitle(x.prev)} {x.prev.ToHexadecimal()} => {UnsafeNative.GetWindowTitle(x.curr)} {x.curr.ToHexadecimal()}"),
                    overlayWindow.WhenLoaded.Select(x => $"[IsVisible {IsVisible}] Processing WhenLoaded event"))
                .Do(reason => Log.Debug($"[{overlayName}] {reason}"))
                .ObserveOn(uiScheduler)
                .SubscribeSafe(reason => HandleVisibilityChange(overlayWindow, viewModel), Log.HandleUiException)
                .AddTo(childAnchors);

            overlayWindow
                .WhenLoaded
                .Select(_ => viewModel.WhenAnyValue(x => x.OverlayMode))
                .Switch()
                .ObserveOn(uiScheduler)
                .SubscribeSafe(overlayWindow.SetOverlayMode, Log.HandleUiException)
                .AddTo(childAnchors);

            overlayWindow
                .WhenRendered
                .Do(_ => { Log.Debug($"[#{overlayWindow.Name}] Overlay is rendered"); })
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(childAnchors);

            windows.Add(overlayWindow);

            Disposable.Create(() =>
            {
                Log.Debug($"[#{overlayWindow.Name}] Closing overlay");
                overlayWindow.Close();
            }).AddTo(childAnchors);
            Disposable.Create(() =>
            {
                Log.Debug($"[#{overlayWindow.Name}] Removing overlay, overlayList: {windows.Items.Select(x => x.Name).ToArray()}");
                windows.Remove(overlayWindow);
            }).AddTo(childAnchors);

            childAnchors.AddTo(Anchors);

            Log.Info($"Overlay #{overlayName} initialized");
            sw.Step($"Registration completed");

            return childAnchors;
        }

        public void ActivateLastActiveWindow()
        {
            var windowHandle = lastActiveWindowHandle.Value;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            UnsafeNative.SetForegroundWindow(windowHandle);
        }

        private void HandleVisibilityChange(OverlayWindowView overlayWindow, IOverlayViewModel viewModel)
        {
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
           
            if (isVisible && viewModel.IsVisible)
            {
                Log.Debug($"[#{overlayWindow.Name}] Showing overlay {overlayWindow}");
                if (overlayWindow.Visibility != Visibility.Visible)
                {
                    Log.Debug($"[#{overlayWindow.Name}] Overlay visibility is {overlayWindow.Visibility}, setting to Visible");
                    overlayWindow.Visibility = Visibility.Visible;
                }

                WindowsServices.ShowInactiveTopmost(overlayWindowHandle, viewModel.NativeBounds);
            } else if (overlayWindowHandle == IntPtr.Zero)
            {
                Log.Debug($"[#{overlayWindow.Name}] Overlay is not initialized yet");
            }
            else
            {
                Log.Debug($"[#{overlayWindow.Name}] Hiding overlay (tracker {windowTracker})");

                WindowsServices.HideWindow(overlayWindowHandle);
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

            Log.Debug($"Overlay controller IsVisible = {IsVisible} => {isVisible} (tracker {windowTracker})");
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
}