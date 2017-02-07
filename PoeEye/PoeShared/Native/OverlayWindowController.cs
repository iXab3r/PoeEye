using System;
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
        private readonly OverlayWindowViewModel overlay;
        private readonly OverlayWindowView overlayWindow;
        private readonly IWindowTracker windowTracker;
        private readonly BehaviorSubject<IntPtr> lastActiveWindowHandle = new BehaviorSubject<IntPtr>(IntPtr.Zero);

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

            overlayWindow = new OverlayWindowView(overlayMode)
            {
                DataContext = overlay,
            };
            overlayWindow.Show();
            var overlayWindowHandle = new WindowInteropHelper(overlayWindow).Handle;

            var application = Application.Current;
            if (application != null)
            {
                Observable
                        .FromEventPattern<ExitEventHandler, ExitEventArgs>(h => application.Exit += h, h => application.Exit -= h)
                        .Subscribe(overlayWindow.Close)
                        .AddTo(Anchors);
            }

            windowTracker
                .WhenAnyValue(x => x.IsActive)
                .Select(x => x || windowTracker.ActiveWindowHandle == overlayWindowHandle)
                .Where(isActive => overlay.IsVisible != isActive)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    isActive =>
                    {
                        overlay.IsVisible = isActive;
                        this.RaisePropertyChanged(nameof(IsVisible));
                    })
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
                .Where(x => new KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift).MatchesHotkey(x))
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
        }

        public bool IsVisible => overlay.IsVisible;

        public Size Size => overlayWindow.RenderSize;

        public Point Location => new Point(overlayWindow.Left, overlayWindow.Top);

        public void ActivateLastActiveWindow()
        {
            var windowHandle = lastActiveWindowHandle.Value;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }
            WindowsServices.SetForegroundWindow(windowHandle);
        }
    }
}