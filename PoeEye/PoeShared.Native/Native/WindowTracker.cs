using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using log4net;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;
using ObservableEx = PoeShared.Scaffolding.ObservableEx;

namespace PoeShared.Native
{
    public class WindowTracker : DisposableReactiveObject, IWindowTracker
    {
        private static readonly IFluentLog Log = typeof(WindowTracker).PrepareLogger();
        private static readonly TimeSpan RecheckPeriod = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan SamplePeriod = TimeSpan.FromMilliseconds(100);
        private readonly IStringMatcher titleMatcher;
        private IntPtr windowHandle;
 

        public WindowTracker(
            IFactory<IWinEventHookWrapper, WinEventHookArguments> hookFactory,
            IStringMatcher titleMatcher,
            [Unity.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(titleMatcher, nameof(titleMatcher));

            this.titleMatcher = titleMatcher;

            var timerObservable = ObservableEx
                .BlockingTimer(RecheckPeriod, timerName: "WndTracker")
                .ToUnit();
            
            var objectFocusHook = hookFactory.Create(new WinEventHookArguments
            {
                Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
                EventMin = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
                EventMax = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
            });

            timerObservable
                .Select(_ => "Timer")
                .Merge(objectFocusHook.WhenWindowEventTriggered.Select(_ => $"{nameof( User32.WindowsEventHookType.EVENT_OBJECT_FOCUS)}"))
                .Sample(SamplePeriod, bgScheduler)
                .Select(_ => UnsafeNative.GetForegroundWindow())
                .Select(hwnd => new { ActiveWindow = hwnd, Title = UnsafeNative.GetWindowTitle(hwnd), ProcessId = UnsafeNative.GetProcessIdByWindowHandle(this.ActiveWindowHandle) })
                .DistinctUntilChanged()
                .SubscribeSafe(x => WindowActivated(x.ActiveWindow, x.Title, x.ProcessId), Log.HandleUiException)
                .AddTo(Anchors);
        }

        public int ExecutingProcessId => Environment.ProcessId;

        public string Name { get; set; }

        public bool IsActive { get; private set; }

        public IntPtr MatchingWindowHandle
        {
            get => windowHandle;
            private set => this.RaiseAndSetIfChanged(ref windowHandle, value);
        }

        public string ActiveWindowTitle { get; private set; }

        public IntPtr ActiveWindowHandle { get; private set; }

        public int ActiveProcessId { get; private set; }

        public override string ToString()
        {
            return $"#Tracker{Name}";
        }

        private void WindowActivated(IntPtr hwnd, string title, int processId)
        {
            var previousState = new {IsActive, MatchingWindowHandle, ActiveWindowTitle, ActiveWindowHandle, ActiveProcessId};
            ActiveWindowHandle = hwnd;
            ActiveWindowTitle = title;
            ActiveProcessId = processId;
            IsActive = titleMatcher.IsMatch(ActiveWindowTitle);
            windowHandle = IsActive ? hwnd : IntPtr.Zero;

            if (previousState.ActiveWindowHandle != ActiveWindowHandle)
            {
                Log.Debug($"[#{Name}] Target window is {(IsActive ? string.Empty : "NOT ")}ACTIVE ({hwnd.ToHexadecimal()}, title '{ActiveWindowTitle}')");
            }

            this.RaiseIfChanged(nameof(IsActive), previousState.IsActive, IsActive);
            this.RaiseIfChanged(nameof(MatchingWindowHandle), previousState.MatchingWindowHandle, MatchingWindowHandle);
            this.RaiseIfChanged(nameof(ActiveWindowTitle), previousState.ActiveWindowTitle, ActiveWindowTitle);
            this.RaiseIfChanged(nameof(ActiveWindowHandle), previousState.ActiveWindowHandle, ActiveWindowHandle);
            this.RaiseIfChanged(nameof(ActiveProcessId), previousState.ActiveProcessId, ActiveProcessId);
        }
    }
}