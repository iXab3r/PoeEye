namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    using PoeShared;
    using PoeShared.Utilities;

    using ReactiveUI;
    using System.Text.RegularExpressions;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Prism;

    internal sealed class WindowTracker : DisposableReactiveObject, IWindowTracker
    {
        private readonly Func<string> titleMatcherRegexFunc;
        private static readonly TimeSpan recheckPeriod = TimeSpan.FromSeconds(1);

        public WindowTracker([NotNull] Func<string> titleMatcherRegexFunc)
        {
            this.titleMatcherRegexFunc = titleMatcherRegexFunc;
            Guard.ArgumentNotNull(() => titleMatcherRegexFunc);

            Observable
              .Timer(DateTimeOffset.Now, recheckPeriod)
              .Select(_ => NativeMethods.GetForegroundWindow())
              .DistinctUntilChanged()
              .Subscribe(WindowActivated)
              .AddTo(Anchors);
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            var activeWindowTitle = NativeMethods.GetWindowTitle(activeWindowHandle);
            Log.Instance.Debug($"[WindowTracker] Active window: '{activeWindowTitle}'");

            var targetTitle = titleMatcherRegexFunc();

            IsActive = !string.IsNullOrWhiteSpace(activeWindowTitle) &&
                       !string.IsNullOrWhiteSpace(targetTitle) &&
                       Regex.IsMatch(activeWindowTitle, targetTitle);

            Log.Instance.DebugFormat("[WindowTracker] Target window is {0}ACTIVE (expected title is '{1}', got '{2}')", isActive ? string.Empty : "NOT ", targetTitle, activeWindowTitle);
        }

        private bool isActive;

        public bool IsActive
        {
            get { return isActive; }
            private set { this.RaiseAndSetIfChanged(ref isActive, value); }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

            public static string GetWindowTitle(IntPtr hwnd)
            {
                const int nChars = 256;
                var buff = new StringBuilder(nChars);

                return GetWindowText(hwnd, buff, nChars) > 0
                    ? buff.ToString()
                    : null;
            }
        }
    }
}