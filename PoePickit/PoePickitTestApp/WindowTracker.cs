using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoePickitTestApp
{
    internal sealed class WindowTracker : DisposableReactiveObject, IWindowTracker
    {
        private static readonly TimeSpan recheckPeriod = TimeSpan.FromSeconds(1);
        private readonly Func<string> titleMatcherRegexFunc;

        private bool isActive;

        public WindowTracker([NotNull] Func<string> titleMatcherRegexFunc)
        {
            Guard.ArgumentNotNull(() => titleMatcherRegexFunc);
            this.titleMatcherRegexFunc = titleMatcherRegexFunc;

            Observable
                .Timer(DateTimeOffset.Now, recheckPeriod)
                .Select(_ => NativeMethods.GetForegroundWindow())
                .DistinctUntilChanged()
                .Subscribe(WindowActivated);
        }

        public bool IsActive
        {
            get { return isActive; }
            private set { this.RaiseAndSetIfChanged(ref isActive, value); }
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            var activeWindowTitle = NativeMethods.GetWindowTitle(activeWindowHandle);

            var targetTitle = titleMatcherRegexFunc();

            IsActive = !string.IsNullOrWhiteSpace(activeWindowTitle) &&
                       !string.IsNullOrWhiteSpace(targetTitle) &&
                       Regex.IsMatch(activeWindowTitle, targetTitle);
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