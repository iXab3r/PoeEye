using System;
using System.Reactive.Linq;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public class WindowTracker : DisposableReactiveObject, IWindowTracker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowTracker));

        private static readonly TimeSpan RecheckPeriod = TimeSpan.FromMilliseconds(250);
        private readonly IStringMatcher titleMatcher;

        private IntPtr activeWindowHandle;
        private string activeWindowTitle;
        private bool isActive;
        private string name;
        private IntPtr windowHandle;

        public WindowTracker(
            [NotNull] IStringMatcher titleMatcher)
        {
            Guard.ArgumentNotNull(titleMatcher, nameof(titleMatcher));

            this.titleMatcher = titleMatcher;

            var timerObservable = Observable
                .Timer(DateTimeOffset.Now, RecheckPeriod)
                .ToUnit();

            timerObservable
                .Select(_ => UnsafeNative.GetForegroundWindow())
                .DistinctUntilChanged()
                .Subscribe(WindowActivated)
                .AddTo(Anchors);
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public bool IsActive
        {
            get => isActive;
            private set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public IntPtr MatchingWindowHandle
        {
            get => windowHandle;
            private set => this.RaiseAndSetIfChanged(ref windowHandle, value);
        }

        public string ActiveWindowTitle
        {
            get => activeWindowTitle;
            private set => this.RaiseAndSetIfChanged(ref activeWindowTitle, value);
        }

        public IntPtr ActiveWindowHandle
        {
            get => activeWindowHandle;
            private set => this.RaiseAndSetIfChanged(ref activeWindowHandle, value);
        }

        public override string ToString()
        {
            return $"#Tracker{Name}";
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            this.activeWindowHandle = activeWindowHandle;
            activeWindowTitle = UnsafeNative.GetWindowTitle(activeWindowHandle);

            isActive = titleMatcher.IsMatch(activeWindowTitle);

            windowHandle = IsActive ? activeWindowHandle : IntPtr.Zero;

            Log.Trace(
                $@"[#{Name}] Target window is {(isActive ? string.Empty : "NOT ")}ACTIVE (hwnd 0x{activeWindowHandle.ToInt64():X8}, active title '{activeWindowTitle}')");

            this.RaisePropertyChanged(nameof(IsActive));
            this.RaisePropertyChanged(nameof(MatchingWindowHandle));
            this.RaisePropertyChanged(nameof(ActiveWindowTitle));
            this.RaisePropertyChanged(nameof(ActiveWindowHandle));
        }
    }
}