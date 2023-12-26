using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
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
    
    /*
     * Performance considerations:
     * EventMin: EVENT_OBJECT_NAMECHANGE, EventMax: EVENT_OBJECT_DESCRIPTIONCHANGE, ProcessId: 0, ThreadId: 0, Flags: WINEVENT_OUTOFCONTEXT
     * About 20-30k events per 5 minutes, processing time < 250ms in total
     *
     * EventMin: EVENT_OBJECT_LOCATIONCHANGE
     * About 50k events per 5 minutes, processing time < 400ms in total
     */
    
    private readonly IScheduler bgScheduler;

    private readonly User32.WinEventProc eventDelegate;
    private readonly WinEventHookArguments hookArgs;

    private readonly WorkerThread hookThread;

    private readonly Subject<WinEventHookData> whenWindowEventTriggered = new();
    private readonly BlockingCollection<WinEventHookData> unprocessedEvents = new();
    private readonly WorkerThread notificationsThread;

#if DEBUG
    private long invocationsCount;
    private long totalProcessingTimeTicks;
    private long totalNotificationsTimeTicks;
    private long errorsCount;
    private TimeSpan TotalProcessingTime => Stopwatch.GetElapsedTime(0, totalProcessingTimeTicks);
    private TimeSpan TotalNotificationsTime => Stopwatch.GetElapsedTime(0, totalNotificationsTimeTicks);
#endif

    public WinEventHookWrapper(
        WinEventHookArguments hookArgs,
        [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
    {
        Log = typeof(WinEventHookWrapper).PrepareLogger().WithSuffix(hookArgs.ToString());
        this.hookArgs = hookArgs;
        this.bgScheduler = bgScheduler;
        eventDelegate = WinEventDelegateProc;
        Log.Debug($"New WinEvent hook created");

        Disposable.Create(() => Log.Info($"Disposing {nameof(WinEventHookWrapper)}")).AddTo(Anchors);
        hookThread = new WorkerThread($"Hook {hookArgs.ToString()}", token => RunHookThread(), autoStart: true).AddTo(Anchors);
        notificationsThread = new WorkerThread($"HookNotifications {hookArgs.ToString()}", token => RunNotificationsThread(), autoStart: true).AddTo(Anchors);
        Disposable.Create(() => Log.Info($"Disposed {nameof(WinEventHookWrapper)}")).AddTo(Anchors);
    }
    private IFluentLog Log { get; }

    public IObservable<WinEventHookData> WhenWindowEventTriggered => whenWindowEventTriggered;

    private void WinEventDelegateProc(IntPtr hWinEventHook,
        User32.WindowsEventHookType @event,
        IntPtr hwnd,
        int idObject,
        int idChild,
        int dwEventThread,
        uint dwmsEventTime)
    {
#if DEBUG
        Interlocked.Increment(ref invocationsCount);
        var startTimestamp = Stopwatch.GetTimestamp();
#endif
        try
        {
            var eventHookData = new WinEventHookData
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
                Log.Debug($"Event hook triggered: {eventHookData}");
            }

            unprocessedEvents.Add(eventHookData);
        }
        catch (Exception e)
        {
            Log.Warn($"Exception in window hook", e);
#if DEBUG
            Interlocked.Increment(ref errorsCount);
#endif
        }
        finally
        {
#if DEBUG
            var endTimestamp = Stopwatch.GetTimestamp();
            var elapsedTimeTicks = endTimestamp - startTimestamp;
            Interlocked.Add(ref totalProcessingTimeTicks, elapsedTimeTicks);
#endif
        }
    }

    private void RunHookThread()
    {
        Log.Info($"Registering the hook");
        RegisterHook().AddTo(Anchors);
        Log.Debug($"Initializing event loop to allow retrieval of hook messages");
        //Even though GetMessage does not directly retrieve hook notifications,
        //having a message loop running in the thread that set the hook is still important.
        //The message loop maintains the thread in a state that allows the OS to invoke the callback function asynchronously when an event occurs.
        EventLoop.RunWindowEventLoop(Log);
    }

    private void RunNotificationsThread()
    {
        Log.Info($"Starting up event notifications loop");
        try
        {
            foreach (var eventHookData in unprocessedEvents.GetConsumingEnumerable())
            {
#if DEBUG
                var startTimestamp = Stopwatch.GetTimestamp();
#endif
                try
                {

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Raising event hook notification(queue: {unprocessedEvents.Count}): {eventHookData}");
                    }

                    whenWindowEventTriggered.OnNext(eventHookData);
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to process Win event hook data: {eventHookData}", e);
                }
                finally
                {
#if DEBUG
                    var endTimestamp = Stopwatch.GetTimestamp();
                    var elapsedTimeTicks = endTimestamp - startTimestamp;
                    Interlocked.Add(ref totalNotificationsTimeTicks, elapsedTimeTicks);
#endif
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("Win event hooks notifications thread has encountered an error", e);
        }
        finally
        {
            Log.Info($"Event notifications loop completed");
        }
    }

    private IDisposable RegisterHook()
    {
        Log.Info($"Registering hook");
            
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

    private static class EventLoop
    {
        private const uint WM_QUIT = 0x0012;

        public static void RunWindowEventLoop(IFluentLog log)
        {
            try
            {
                log.Info($"Event loop started");
                long messagesProcessed = 0;
                while(GetMessage(out var msg, IntPtr.Zero, 0, 0 ))
                {
                    try
                    {
                        if (msg.Message == WM_QUIT)
                        {
                            log.Info($"Received {nameof(WM_QUIT)}, breaking event loop");
                            break;
                        }

                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                    catch (Exception e)
                    {
                        log.Warn($"Exception in EventLoop", e);
                    }
                    finally
                    {
                        messagesProcessed++;
                    }
                } 
            }
            catch (Exception e)
            {
                log.Error($"Unhandled exception in EventLoop thread", e);
            }
            finally
            {
                log.Info($"Event hook loop completed");
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