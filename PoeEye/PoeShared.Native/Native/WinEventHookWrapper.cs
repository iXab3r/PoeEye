using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using log4net;
using PInvoke;
using PoeShared.Scaffolding;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace PoeShared.Native
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public sealed class WinEventHookWrapper : DisposableReactiveObject, IWinEventHookWrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WinEventHookWrapper));

        private readonly WinEventHookArguments hookArgs;
        private readonly ISubject<IntPtr> whenWindowEventTriggered = new Subject<IntPtr>();
        private readonly User32.WinEventProc eventDelegate;

        public WinEventHookWrapper(WinEventHookArguments hookArgs)
        {
            this.hookArgs = hookArgs;
            eventDelegate = WinEventDelegateProc;
            Log.Debug($"New WinEvent hook created, args: {hookArgs}");

            Disposable.Create(() => Log.Info($"Disposing WinEventHookWrapper")).AddTo(Anchors);
            var hookEventLoopTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning).AddTo(Anchors);
        }

        public IObservable<IntPtr> WhenWindowEventTriggered => whenWindowEventTriggered;

        private void WinEventDelegateProc(User32.SafeEventHookHandle hWinEventHook,
            User32.WindowsEventHookType @event,
            IntPtr hwnd,
            int idObject,
            int idChild,
            int dwEventThread,
            uint dwmsEventTime)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"[{hookArgs}] Event hook triggered, hWinEventHook: {hWinEventHook.DangerousGetHandle().ToHexadecimal()}, eventType: {@event}, hwnd: {hwnd.ToHexadecimal()}, idObject: {idObject}, idChild: {idChild}, dwEventThread: {dwEventThread}, dwmsEventTime: {dwmsEventTime}");
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
            
            var hook = User32.SetWinEventHook(
                hookArgs.EventMin,
                hookArgs.EventMax,
                IntPtr.Zero,
                eventDelegate,
                hookArgs.ProcessId,
                hookArgs.ThreadId,
                hookArgs.Flags);
            Log.Debug($"Hook handle(args: {hookArgs}): {hook.DangerousGetHandle().ToHexadecimal()}");

            if (hook.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Disposable.Create(() =>
            {
                Log.Debug($"Unregistering hook (args: {hookArgs}) {hook.DangerousGetHandle().ToHexadecimal()}");
                hook.DangerousRelease();
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