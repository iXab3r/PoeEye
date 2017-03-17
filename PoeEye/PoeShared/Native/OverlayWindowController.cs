using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowController : DisposableReactiveObject, IOverlayWindowController
    {
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private readonly BehaviorSubject<IntPtr> lastActiveWindowHandle = new BehaviorSubject<IntPtr>(IntPtr.Zero);

        private readonly string[] possibleOverlayNames;
        private readonly IWindowTracker windowTracker;
        private readonly IFactory<OverlayWindowViewModel> overlayWindowViewModelFactory;
        private readonly OverlayMode overlayMode;

        public OverlayWindowController(
            [NotNull] IWindowTracker windowTracker,
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IFactory<OverlayWindowViewModel> overlayWindowViewModelFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            OverlayMode overlayMode = OverlayMode.Transparent)
        {
            Guard.ArgumentNotNull(() => windowTracker);
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.windowTracker = windowTracker;
            this.overlayWindowViewModelFactory = overlayWindowViewModelFactory;
            this.overlayMode = overlayMode;

            possibleOverlayNames = Enum.GetValues(typeof(OverlayMode))
                .OfType<OverlayMode>()
                .Select(mode => $"[PoeEye.{mode}] {windowTracker.TargetWindowName}")
                .ToArray();

            var application = Application.Current;
            if (application != null)
            {
                var applicationExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(
                        h => application.Exit += h,
                        h => application.Exit -= h)
                    .ToUnit();

                var mainWindow = application.MainWindow;
                var mainWindowClosed = mainWindow == null
                    ? Observable.Never<Unit>()
                    : Observable.FromEventPattern<EventHandler, EventArgs>(
                            h => mainWindow.Closed += h,
                            h => mainWindow.Closed -= h)
                        .ToUnit();

                Observable.Merge(mainWindowClosed, applicationExit)
                    .Subscribe(CloseOverlays, Log.HandleUiException)
                    .AddTo(Anchors);
            }

            windowTracker.WhenAnyValue(x => x.ActiveWindowHandle)
                .Select(x => new
                {
                    WindowIsActive = windowTracker.IsActive,
                    OverlayIsActive = IsPairedOverlay(windowTracker.ActiveWindowTitle),
                    ActiveTitle = windowTracker.ActiveWindowTitle
                })
                .Do(x => Log.Instance.Debug($"[OverlayWindowController] Active window has changed: {x}"))
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
                .Subscribe(() => ShowWireframes = !ShowWireframes)
                .AddTo(Anchors);
        }

        private bool isVisible;

        public bool IsVisible
        {
            get { return isVisible; }
            set { this.RaiseAndSetIfChanged(ref isVisible, value); }
        }

        private bool showWireframes;

        public bool ShowWireframes
        {
            get { return showWireframes; }
            set { this.RaiseAndSetIfChanged(ref showWireframes, value); }
        }

        public IReactiveList<OverlayWindowView> Overlays { get; } = new ReactiveList<OverlayWindowView>();

        public void RegisterChild(IOverlayViewModel viewModel)
        {
            Guard.ArgumentNotNull(() => viewModel);

            var overlayWindowViewModel = new OverlayWindowViewModel()
            {
                Content = viewModel,
            };
            var overlayWindow = new OverlayWindowView(overlayMode)
            {
                DataContext = overlayWindowViewModel,
                Title = $"[PoeEye.{overlayMode}] {windowTracker.TargetWindowName}",
                Visibility = Visibility.Visible,
                Topmost = true,
            };

            var observer = viewModel.WhenLoaded as IObserver<Unit>;
            if (observer != null)
            {
                //FIXME Inheritance problem
                overlayWindow.WhenLoaded.Subscribe(observer).AddTo(Anchors);
            }

            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;

            Log.Instance.Debug(
                $"[OverlayWindowController..ctor] Overlay window({overlayMode} + {windowTracker}) handle: 0x{overlayWindowHandle.ToInt64():x8}");

            this.WhenAnyValue(x => x.IsVisible)
                .Select(_ => overlayWindow)
                .Subscribe(x => HandleVisibilityChange(x, viewModel))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ShowWireframes)
               .Subscribe(x => overlayWindowViewModel.ShowWireframes = x)
               .AddTo(Anchors);
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

        private void CloseOverlays()
        {
            Overlays.ForEach(x => x.Close());
        }

        private void HandleVisibilityChange(OverlayWindowView overlayWindow, IOverlayViewModel viewModel)
        {
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
            if (isVisible)
            {
                WindowsServices.ShowInactiveTopmost(
                    overlayWindowHandle,
                    (int)viewModel.Left,
                    (int)viewModel.Top,
                    (int)viewModel.Width,
                    (int)viewModel.Height);
            }
            else
            {
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
            Log.Instance.Debug($"[OverlayWindowController] Overlay IsVisible = {IsVisible} => {isVisible} (tracker {windowTracker})");
            IsVisible = isVisible;
        }
    }
}