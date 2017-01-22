using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guards;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    internal sealed class WindowTracker : DisposableReactiveObject, IWindowTracker
    {
        private static readonly TimeSpan MinRecheckPeriod = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan RecheckPeriod = TimeSpan.FromMilliseconds(1000);
        private readonly Func<string> titleMatcherRegexFunc;
        private readonly WinEventHookWrapper winHook = new WinEventHookWrapper();

        private IntPtr windowHandle;

        private bool isActive;

        public WindowTracker([NotNull] Func<string> titleMatcherRegexFunc)
        {
            Guard.ArgumentNotNull(() => titleMatcherRegexFunc);
            this.titleMatcherRegexFunc = titleMatcherRegexFunc;

            var timerObservable = Observable
                .Timer(DateTimeOffset.Now, RecheckPeriod)
                .ToUnit();
            var hookObservable = winHook
                .WhenWindowLocationChanged
                .Sample(MinRecheckPeriod)
                .ToUnit();

            Observable.Merge(
                    timerObservable,
                    hookObservable)
                .Select(_ => NativeMethods.GetForegroundWindow())
                .DistinctUntilChanged()
                .Subscribe(WindowActivated)
                .AddTo(Anchors);
        }

        public bool IsActive
        {
            get { return isActive; }
            private set { this.RaiseAndSetIfChanged(ref isActive, value); }
        }

        public IntPtr WindowHandle
        {
            get { return windowHandle; }
            private set { this.RaiseAndSetIfChanged(ref windowHandle, value); }
        }

        private string activeWindowTitle;

        public string ActiveWindowTitle
        {
            get { return activeWindowTitle; }
            set { this.RaiseAndSetIfChanged(ref activeWindowTitle, value); }
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            var targetTitle = titleMatcherRegexFunc();
            activeWindowTitle = NativeMethods.GetWindowTitle(activeWindowHandle);

            isActive = !string.IsNullOrWhiteSpace(activeWindowTitle) &&
                   !string.IsNullOrWhiteSpace(targetTitle) &&
                   Regex.IsMatch(activeWindowTitle, targetTitle, RegexOptions.IgnoreCase);

            windowHandle = IsActive ? activeWindowHandle : IntPtr.Zero;

            this.RaisePropertyChanged(nameof(IsActive));
            this.RaisePropertyChanged(nameof(WindowHandle));
            this.RaisePropertyChanged(nameof(ActiveWindowTitle));

            Log.Instance.DebugFormat(
                "[WindowTracker] Target window is {0}ACTIVE (hwnd 0x{3:8X}  expected title is '{1}', got '{2}')",
                isActive ? string.Empty : "NOT ",
                targetTitle,
                activeWindowTitle,
                windowHandle.ToInt64());
        }

        private static class NativeMethods
        {
            public const uint WINEVENT_OUTOFCONTEXT = 0;
            public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
                WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hHook);

            public delegate void WinEventDelegate(
                IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
                uint dwmsEventTime);

            public static string GetWindowTitle(IntPtr hwnd)
            {
                const int nChars = 256;
                var buff = new StringBuilder(nChars);

                return GetWindowText(hwnd, buff, nChars) > 0
                    ? buff.ToString()
                    : null;
            }
        }

        private sealed class WinEventHookWrapper : DisposableReactiveObject
        {
            private readonly NativeMethods.WinEventDelegate resizeEventDelegate;
            private readonly ISubject<IntPtr> whenWindowLocationChanged = new Subject<IntPtr>();

            public WinEventHookWrapper()
            {
                resizeEventDelegate = WinResizeEventProc;
                Task.Run(() => this.Run());
            }

            public IObservable<IntPtr> WhenWindowLocationChanged => whenWindowLocationChanged;

            private void WinResizeEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
                int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                whenWindowLocationChanged.OnNext(hwnd);
            }

            private void Run()
            {
                RegisterHook().AddTo(Anchors);
                Application.Run();
            }

            private IDisposable RegisterHook()
            {
                var hook = NativeMethods.SetWinEventHook(
                   NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                   NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                   IntPtr.Zero,
                   resizeEventDelegate,
                   0,
                   0,
                   NativeMethods.WINEVENT_OUTOFCONTEXT);

                return Disposable.Create(() => NativeMethods.UnhookWinEvent(hook));
            }
        }
    }
}