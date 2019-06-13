using System;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public sealed class WinEventHookWrapper : DisposableReactiveObject, IWinEventHookWrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WinEventHookWrapper));
        private readonly UnsafeNative.WinEventDelegate eventDelegate;

        private readonly WinEventHookArguments hookArgs;
        private readonly ISubject<IntPtr> whenWindowEventTriggered = new Subject<IntPtr>();

        public WinEventHookWrapper(WinEventHookArguments hookArgs)
        {
            this.hookArgs = hookArgs;
            eventDelegate = WinEventDelegateProc;
            Log.Debug($"New WinEvent hook created, args: {hookArgs}");

            Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        public IObservable<IntPtr> WhenWindowEventTriggered => whenWindowEventTriggered;

        private void WinEventDelegateProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
            int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"[{hookArgs}] Event hook triggered, hWinEventHook: 0x{hWinEventHook.ToInt64():x8}, eventType: {eventType}, hwnd: 0x{hwnd.ToInt64():x8}, idObject: {idObject}, idChild: {idChild}, dwEventThread: {dwEventThread}, dwmsEventTime: {dwmsEventTime}");
            }

            whenWindowEventTriggered.OnNext(hwnd);
        }

        private void Run()
        {
            Log.Debug("Starting up event sink");
            RegisterHook().AddTo(Anchors);
            Log.Debug("Initializing sink");

            EventLoop.Run();
        }

        private IDisposable RegisterHook()
        {
            Log.Debug($"Registering hook, args: {hookArgs}");
            var hook = UnsafeNative.SetWinEventHook(
                hookArgs.EventMin,
                hookArgs.EventMax,
                IntPtr.Zero,
                eventDelegate,
                hookArgs.ProcessId,
                hookArgs.ThreadId,
                hookArgs.Flags);
            Log.Debug($"Hook handle(args: {hookArgs}): {hook.ToInt64():x8}");

            if (hook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Disposable.Create(() =>
            {
                Log.Debug($"Unregistering hook (args: {hookArgs}) {hook.ToInt64():x8}");

                UnsafeNative.UnhookWinEvent(hook);
            });
        }


        public static class EventLoop
        {
            private const uint PM_NOREMOVE = 0;
            private const uint PM_REMOVE = 1;

            private const uint WM_QUIT = 0x0012;

            public static void Run()
            {
                MSG msg;

                while (true)
                {
                    if (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                    {
                        if (msg.Message == WM_QUIT)
                        {
                            break;
                        }

                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                    else
                    {
                        WaitMessage();
                    }
                }
            }

            [DllImport("user32.dll")]
            private static extern bool PeekMessage(out MSG lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

            [DllImport("user32.dll")]
            private static extern bool TranslateMessage(ref MSG lpMsg);

            [DllImport("user32.dll")]
            private static extern IntPtr DispatchMessage(ref MSG lpMsg);

            [DllImport("user32.dll")]
            private static extern bool WaitMessage();

            [StructLayout(LayoutKind.Sequential)]
            private struct MSG
            {
                public readonly IntPtr Hwnd;
                public readonly uint Message;
                public readonly IntPtr WParam;
                public readonly IntPtr LParam;
                public readonly uint Time;
                public readonly Point Point;
            }
        }
    }
}