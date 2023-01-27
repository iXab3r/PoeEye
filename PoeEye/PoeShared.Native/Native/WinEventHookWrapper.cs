using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PInvoke;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Unity;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace PoeShared.Native;

[SuppressMessage("ReSharper", "IdentifierTypo")]
public sealed class WinEventHookWrapper : DisposableReactiveObject, IWinEventHookWrapper
{
    private readonly IScheduler bgScheduler;

    private readonly User32.WinEventProc eventDelegate;
    private readonly WinEventHookArguments hookArgs;

    private readonly ISubject<WinEventHookData> whenWindowEventTriggered = new Subject<WinEventHookData>();

    public WinEventHookWrapper(
        WinEventHookArguments hookArgs,
        [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
    {
        Log = typeof(WinEventHookWrapper).PrepareLogger().WithSuffix(hookArgs.ToString());
        this.hookArgs = hookArgs;
        this.bgScheduler = bgScheduler;
        eventDelegate = WinEventDelegateProc;
        Log.Debug(() => $"New WinEvent hook created, args: {hookArgs}");

        Disposable.Create(() => Log.Info(() => $"Disposing {nameof(WinEventHookWrapper)}")).AddTo(Anchors);
        Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning).AddTo(Anchors);
        Disposable.Create(() => Log.Info(() => $"Disposed {nameof(WinEventHookWrapper)}")).AddTo(Anchors);
    }
    private IFluentLog Log { get; }

    public IObservable<WinEventHookData> WhenWindowEventTriggered => whenWindowEventTriggered.Synchronize().ObserveOn(bgScheduler);

    private void WinEventDelegateProc(IntPtr hWinEventHook,
        User32.WindowsEventHookType @event,
        IntPtr hwnd,
        int idObject,
        int idChild,
        int dwEventThread,
        uint dwmsEventTime)
    {
        try
        {
            var data = new WinEventHookData
            {
                EventId = @event,
                WindowHandle = hwnd,
                ChildId = idChild,
                ObjectId = idObject,
                EventThreadId = dwEventThread,
                EventTimeInMs = dwmsEventTime,
                WinEventHookHandle = hWinEventHook
            };
                
            if (Log.IsDebugEnabled)
            {
                Log.Debug(() => $"[{hookArgs}] Event hook triggered: {data}");
            }
            whenWindowEventTriggered.OnNext(data);
        }
        catch (Exception e)
        {
            Log.Warn($"Exception in window hook, args: {hookArgs}", e);
        }
    }

    private void Run()
    {
        Log.Info(() => $"Starting up event sink, args: {hookArgs}");
        RegisterHook().AddTo(Anchors);
        Log.Debug(() => $"Initializing event sink, args: {hookArgs}");
        EventLoop.RunWindowEventLoop(Log);
    }

    private IDisposable RegisterHook()
    {
        Log.Info(() => $"Registering hook, args: {hookArgs}");
            
        var hook = User32.SetWinEventHook(
            hookArgs.EventMin,
            hookArgs.EventMax,
            IntPtr.Zero,
            eventDelegate,
            hookArgs.ProcessId,
            hookArgs.ThreadId,
            hookArgs.Flags);
        Log.Debug(() => $"Hook handle(args: {hookArgs}): {hook.DangerousGetHandle().ToHexadecimal()}");

        if (hook.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return Disposable.Create(() =>
        {
            Log.Debug(() => $"Unregistering hook (args: {hookArgs}) {hook.DangerousGetHandle().ToHexadecimal()}");
            hook.DangerousRelease();
        });
    }

    private static class EventLoop
    {
        private const uint WM_QUIT = 0x0012;

        public static void RunWindowEventLoop(IFluentLog log)
        {
            try
            {
                log.Info(() => $"Event loop started");
                while(GetMessage(out var msg, IntPtr.Zero, 0, 0 ))
                { 
                    try
                    {
                        if (msg.Message == WM_QUIT)
                        {
                            log.Info(() => $"Received {nameof(WM_QUIT)}, breaking event loop");
                            break;
                        }
                        TranslateMessage(ref msg); 
                        DispatchMessage(ref msg); 
                    }
                    catch (Exception e)
                    {
                        log.Warn($"Exception in EventLoop", e);
                    }
                        
                } 
            }
            catch (Exception e)
            {
                log.Error($"Unhandled exception in EventLoop thread", e);
            }
            finally
            {
                log.Info(() => $"Event loop completed");
            }
        }
            
        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

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