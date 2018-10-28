using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;
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
        private readonly WinEventHookWrapper winHook = new WinEventHookWrapper();

        private IntPtr activeWindowHandle;
        private string activeWindowTitle;
        private bool isActive;
        private IntPtr windowHandle;
        private string name;

        public WindowTracker([NotNull] IStringMatcher titleMatcher)
        {
            Guard.ArgumentNotNull(titleMatcher, nameof(titleMatcher));
            this.titleMatcher = titleMatcher;

            winHook.AddTo(Anchors);
            var timerObservable = Observable
                .Timer(DateTimeOffset.Now, RecheckPeriod)
                .ToUnit();
            var hookObservable = winHook
                .WhenWindowLocationChanged
                .ToUnit();

            timerObservable.Merge(hookObservable)
                .Sample(RecheckPeriod)
                .Select(_ => NativeMethods.GetForegroundWindow())
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
            activeWindowTitle = NativeMethods.GetWindowTitle(activeWindowHandle);

            isActive = titleMatcher.IsMatch(activeWindowTitle);

            windowHandle = IsActive ? activeWindowHandle : IntPtr.Zero;

            Log.Trace($@"[#{Name}] Target window is {(isActive ? string.Empty : "NOT ")}ACTIVE (hwnd 0x{activeWindowHandle.ToInt64():X8}, active title '{activeWindowTitle}')");

            this.RaisePropertyChanged(nameof(IsActive));
            this.RaisePropertyChanged(nameof(MatchingWindowHandle));
            this.RaisePropertyChanged(nameof(ActiveWindowTitle));
            this.RaisePropertyChanged(nameof(ActiveWindowHandle));
        }
        
        private static class NativeMethods
        {
            public delegate void WinEventDelegate(
                IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
                uint dwmsEventTime);

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
                Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
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