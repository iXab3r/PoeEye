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

        private readonly OverlayWindowViewModel overlay;
        private readonly OverlayWindowView overlayWindow;

        private readonly string[] possibleOverlayNames;
        private readonly IWindowTracker windowTracker;

        public OverlayWindowController(
            [NotNull] IWindowTracker windowTracker,
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] OverlayWindowViewModel overlay,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            OverlayMode overlayMode = OverlayMode.Transparent)
        {
            Guard.ArgumentNotNull(() => windowTracker);
            Guard.ArgumentNotNull(() => overlay);
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.overlay = overlay;
            this.windowTracker = windowTracker;

            possibleOverlayNames = Enum.GetValues(typeof(OverlayMode))
                .OfType<OverlayMode>()
                .Select(mode => $"[PoeEye.{mode}] {windowTracker.TargetWindowName}")
                .ToArray();

            overlayWindow = new OverlayWindowView(overlayMode)
            {
                DataContext = overlay,
                Title = $"[PoeEye.{overlayMode}] {windowTracker.TargetWindowName}"
            };
            overlayWindow.Show();

            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
            Log.Instance.Debug(
                $"[OverlayWindowController..ctor] Overlay window({overlayMode} + {windowTracker}) handle: 0x{overlayWindowHandle.ToInt64():x8}");

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

                mainWindowClosed.Merge(applicationExit)
                    .Subscribe(overlayWindow.Close, Log.HandleUiException)
                    .AddTo(Anchors);
            }

            overlay
                .WhenAnyValue(x => x.IsVisible)
                .Subscribe(x => this.RaisePropertyChanged(nameof(IsVisible)))
                .AddTo(Anchors);

            windowTracker.WhenAnyValue(x => x.IsActive)
                .CombineLatest(
                    windowTracker.WhenAnyValue(x => x.ActiveWindowTitle).Select(IsPairedOverlay),
                    (x, overlayIsActive) => new {WindowIsActive = x, OverlayIsActive = overlayIsActive}
                )
                .Select(x => x.WindowIsActive || x.OverlayIsActive)
                .DistinctUntilChanged()
                .ObserveOn(uiScheduler)
                .Subscribe(SetVisibility, Log.HandleUiException)
                .AddTo(Anchors);

            windowTracker
                .WhenAnyValue(x => x.MatchingWindowHandle)
                .Where(x => x != IntPtr.Zero && x != overlayWindowHandle)
                .Subscribe(lastActiveWindowHandle)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Where(x => overlay.IsVisible)
                .Where(
                    x =>
                        new KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)
                            .MatchesHotkey(x))
                .Do(x => x.Handled = true)
                .Subscribe(() => overlay.ShowWireframes = !overlay.ShowWireframes)
                .AddTo(Anchors);
        }

        public void RegisterChild(IOverlayViewModel viewModel)
        {
            Guard.ArgumentNotNull(() => viewModel);

            overlay.Items.Add(viewModel);
        }

        public void Activate()
        {
            overlayWindow.Activate();
            if (overlay.IsVisible)
            {
                var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;
                WindowsServices.SetForegroundWindow(overlayWindowHandle);
            }
        }

        public bool IsVisible => overlay.IsVisible;

        public Size Size => overlayWindow.RenderSize;

        public void ActivateLastActiveWindow()
        {
            var windowHandle = lastActiveWindowHandle.Value;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }
            WindowsServices.SetForegroundWindow(windowHandle);
        }

        public Size MinSize => new Size(overlayWindow.MinWidth, overlayWindow.MinHeight);

        public Size MaxSize => new Size(overlayWindow.MaxWidth, overlayWindow.MaxHeight);

        public bool IsLocked { get; } = true;

        public object Header { get; } = null;

        private bool IsPairedOverlay(string activeWindowTitle)
        {
            return possibleOverlayNames.Contains(activeWindowTitle, StringComparer.OrdinalIgnoreCase);
        }

        private void SetVisibility(bool isVisible)
        {
            Log.Instance.Debug(
                $"[OverlayWindowController] Overlay '{overlayWindow}'.IsVisible = {overlay.IsVisible} => {isVisible} (tracker {windowTracker})");
            overlay.IsVisible = isVisible;
        }

        public double Width => overlayWindow.ActualWidth;

        public double Height => overlayWindow.ActualHeight;
        public double Left => overlayWindow.Left;
        public double Top => overlayWindow.Top;
    }
}