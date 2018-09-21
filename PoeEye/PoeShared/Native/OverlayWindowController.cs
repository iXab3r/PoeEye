using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Common.Logging;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowController : DisposableReactiveObject, IOverlayWindowController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayWindowController));

        private readonly ISourceList<OverlayWindowView> windows = new SourceList<OverlayWindowView>();
        private readonly BehaviorSubject<IntPtr> lastActiveWindowHandle = new BehaviorSubject<IntPtr>(IntPtr.Zero);

        private readonly string[] possibleOverlayNames;
        private readonly IScheduler uiScheduler;
        private readonly IWindowTracker windowTracker;

        private bool isVisible;

        private bool showWireframes;

        public OverlayWindowController(
            [NotNull] IWindowTracker windowTracker,
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(windowTracker, nameof(windowTracker));
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.windowTracker = windowTracker;
            this.uiScheduler = uiScheduler;

            possibleOverlayNames = new[]
            {
                $"[PoeEye.Overlay] {windowTracker.TargetWindowName}"
            };

            windowTracker.WhenAnyValue(x => x.ActiveWindowHandle)
                         .Select(
                             x => new
                             {
                                 WindowIsActive = windowTracker.IsActive,
                                 OverlayIsActive = IsPairedOverlay(windowTracker.ActiveWindowTitle),
                                 ActiveTitle = windowTracker.ActiveWindowTitle
                             })
                         .Do(x => Log.Trace($"[OverlayWindowController] Active window has changed: {x}"))
                         .Select(x => x.WindowIsActive || x.OverlayIsActive)
                         .DistinctUntilChanged()
                         .ObserveOn(uiScheduler)
                         .Subscribe(SetVisibility, Log.HandleUiException)
                         .AddTo(Anchors);

            windowTracker
                .WhenAnyValue(x => x.MatchingWindowHandle)
                .Where(x => x != IntPtr.Zero && !IsPairedOverlay(windowTracker.ActiveWindowTitle))
                .Subscribe(lastActiveWindowHandle)
                .AddTo(Anchors);

            keyboardMouseEvents
                .WhenKeyDown
                .Where(x => IsVisible)
                .Where(x => new KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt).MatchesHotkey(x))
                .Do(x => x.Handled = true)
                .ObserveOn(uiScheduler)
                .Subscribe(() => ShowWireframes = !ShowWireframes)
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
            Guard.ArgumentNotNull(viewModel, nameof(viewModel));

            var childAnchors = new CompositeDisposable();

            viewModel.AddTo(childAnchors);

            var overlayWindowViewModel = new OverlayWindowViewModel
            {
                Content = viewModel
            };
            overlayWindowViewModel.AddTo(childAnchors);

            var overlayName = $"{viewModel.GetType().Name}";
            var overlayWindow = new OverlayWindowView
            {
                DataContext = overlayWindowViewModel,
                Title = $"[PoeEye.Overlay] {windowTracker.TargetWindowName} #{overlayName}",
                Visibility = Visibility.Visible,
                Topmost = true,
                Name = $"{overlayName}_OverlayView"
            };
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
            Log.Debug(
                $"[OverlayWindowController #{overlayName}] Created Overlay window({windowTracker}) handle: 0x{overlayWindowHandle.ToInt64():x8}");

            var activationController = new ActivationController(overlayWindow);
            viewModel.SetActivationController(activationController);

            this.WhenAnyValue(x => x.IsVisible)
                .Do(x => { })
                .ObserveOn(uiScheduler)
                .Subscribe(isVisible => HandleVisibilityChange(overlayWindow, viewModel))
                .AddTo(childAnchors);

            this.WhenAnyValue(x => x.ShowWireframes)
                .ObserveOn(uiScheduler)
                .Subscribe(showWireframes => overlayWindowViewModel.ShowWireframes = showWireframes)
                .AddTo(childAnchors);

            viewModel
                .WhenAnyValue(x => x.OverlayMode)
                .ObserveOn(uiScheduler)
                .Subscribe(overlayWindow.SetOverlayMode)
                .AddTo(childAnchors);

            //FIXME Inheritance problem
            if (viewModel.WhenLoaded is IObserver<Unit> observer)
            {
                overlayWindow.WhenLoaded.Subscribe(observer).AddTo(childAnchors);
            }

            windows.Add(overlayWindow);

            Disposable.Create(() => overlayWindow.Close()).AddTo(childAnchors);
            Disposable.Create(() => windows.Remove(overlayWindow)).AddTo(childAnchors);
            
            childAnchors.AddTo(Anchors);

            return childAnchors;
        }

        public void ActivateLastActiveWindow()
        {
            var windowHandle = lastActiveWindowHandle.Value;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            WindowsServices.SetForegroundWindow(windowHandle);
        }

        private void HandleVisibilityChange(OverlayWindowView overlayWindow, IOverlayViewModel viewModel)
        {
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
            if (isVisible)
            {
                Log.Trace($"[OverlayWindowController #{overlayWindow.Name}] Showing overlay (tracker {windowTracker})");

                WindowsServices.ShowInactiveTopmost(
                    overlayWindowHandle,
                    (int)viewModel.Left,
                    (int)viewModel.Top,
                    (int)viewModel.Width,
                    (int)viewModel.Height);
            }
            else
            {
                Log.Trace($"[OverlayWindowController #{overlayWindow.Name}] Hiding overlay (tracker {windowTracker})");

                WindowsServices.HideWindow(overlayWindowHandle);
            }
        }

        private bool IsPairedOverlay(string activeWindowTitle)
        {
            if (string.IsNullOrWhiteSpace(activeWindowTitle))
            {
                return false;
            }

            return possibleOverlayNames
                .Any(activeWindowTitle.Contains);
        }

        private void SetVisibility(bool isVisible)
        {
            if (isVisible == IsVisible)
            {
                return;
            }
            Log.Trace($"[OverlayWindowController] Overlay controller IsVisible = {IsVisible} => {isVisible} (tracker {windowTracker})");
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
                WindowsServices.SetForegroundWindow(overlayWindowHandle);
            }
        }
    }
}