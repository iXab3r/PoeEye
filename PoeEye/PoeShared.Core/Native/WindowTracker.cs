using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using log4net;
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
        private uint activeProcessId;

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
                .StartWithDefault()
                .DistinctUntilChanged()
                .Subscribe(WindowActivated, Log.HandleUiException)
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

        public uint ActiveProcessId
        {
            get => activeProcessId;
            private set => this.RaiseAndSetIfChanged(ref activeProcessId, value);
        }

        public override string ToString()
        {
            return $"#Tracker{Name}";
        }

        private void WindowActivated(IntPtr activeHwnd)
        {
            var previousState = new {IsActive, MatchingWindowHandle, ActiveWindowTitle, ActiveWindowHandle, ActiveProcessId};
            activeWindowHandle = activeHwnd;
            activeWindowTitle = UnsafeNative.GetWindowTitle(activeHwnd);

            isActive = titleMatcher.IsMatch(activeWindowTitle);

            windowHandle = IsActive ? activeHwnd : IntPtr.Zero;
            activeProcessId = UnsafeNative.GetProcessIdByWindowHandle(this.activeWindowHandle);

            if (previousState.ActiveWindowHandle != ActiveWindowHandle)
            {
                Log.Debug($"[#{Name}] Target window is {(isActive ? string.Empty : "NOT ")}ACTIVE (0x{activeHwnd.ToInt64():X8}, title '{activeWindowTitle}')");
            }

            this.RaiseIfChanged(nameof(IsActive), previousState.IsActive, IsActive);
            this.RaiseIfChanged(nameof(MatchingWindowHandle), previousState.MatchingWindowHandle, MatchingWindowHandle);
            this.RaiseIfChanged(nameof(ActiveWindowTitle), previousState.ActiveWindowTitle, ActiveWindowTitle);
            this.RaiseIfChanged(nameof(ActiveWindowHandle), previousState.ActiveWindowHandle, ActiveWindowHandle);
            this.RaiseIfChanged(nameof(ActiveProcessId), previousState.ActiveProcessId, ActiveProcessId);
        }
    }
}