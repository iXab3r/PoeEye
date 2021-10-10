// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.Implementation;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace WindowsHook.WinApi
{
    public sealed class HookResultWithCallback : HookResult
    {
        // ReSharper disable once NotAccessedField.Local
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly User32.WindowsHookDelegate hookProcedure;

        public HookResultWithCallback(User32.WindowsHookType hookType, WinHookCallback callback) : base(hookType)
        {
            Callback = callback;
            hookProcedure = HandleHookCallback;
            Handle = PrepareHandle(Log, hookType, NativeThreadId, hookProcedure);
        }

        public WinHookCallback Callback { get; }

        private int HandleHookCallback(int ncode, IntPtr wparam, IntPtr lparam)
        {
            return HandleHook(ncode, wparam, lparam, this);
        }

        private static int HandleHook(int nCode, IntPtr wParam, IntPtr lParam, HookResultWithCallback owner)
        {
            // If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx.
            // If nCode is greater than or equal to zero, and the hook procedure did not process the message, it is highly recommended that you call CallNextHookEx and return the value it returns;
            // otherwise, other applications that have installed WH_KEYBOARD_LL hooks will not receive hook notifications and may behave incorrectly as a result.
            // If the hook procedure processed the message, it may return a nonzero value to prevent the system from passing the message to the rest of the hook chain or the target window procedure.
            // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)
            if (nCode != 0)
            {
                return CallNextHookEx(nCode, wParam, lParam);
            }

            var callbackData = new WinHookCallbackData(nCode, wParam, lParam);
            
            var continueProcessing = owner.Callback(callbackData);

            if (!continueProcessing)
            {
                return -1;
            }

            var lastHookResult = CallNextHookEx(nCode, wParam, lParam);
            return lastHookResult;
        }
    }

    public sealed class HookResultWithProcedure : HookResult
    {
        // ReSharper disable once NotAccessedField.Local
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly User32.WindowsHookDelegate hookProcedure;

        public HookResultWithProcedure(User32.WindowsHookType hookType, User32.WindowsHookDelegate hookProcedure) : base(hookType)
        {
            this.hookProcedure = hookProcedure;
            Handle = PrepareHandle(Log, hookType, NativeThreadId, hookProcedure);
        }
    }
    
    public abstract class HookResult : DisposableReactiveObject
    {
        private static readonly IFluentLog SharedLog = typeof(HookResult).PrepareLogger();
        private static readonly IntPtr BaseAddress;
        private static long GlobalHookResultId;

        static HookResult()
        {
            SharedLog.Debug("Initializing HookHelper");
            var process = Process.GetCurrentProcess();
            var mainModule = process.MainModule ?? throw new NotSupportedException($"Failed to get MainModule in {process}");
            BaseAddress = mainModule.BaseAddress;
            SharedLog.Debug($"Application base address: {BaseAddress.ToHexadecimal()}");
        }

        protected HookResult(User32.WindowsHookType hookType)
        {
            Log = SharedLog.WithSuffix(ToString);

            Disposable.Create(() => Log.Debug("Disposing hook")).AddTo(Anchors);
            HookType = hookType;
            NativeThreadId = ThreadNativeMethods.GetCurrentThreadId();
            ManagedThreadId = $"{Thread.CurrentThread.ManagedThreadId}-{Thread.CurrentThread.Name}";
            Log.Debug($"Initializing new hook of type {hookType}");
            
            Disposable.Create(() =>
            {
                if (Handle == null)
                {
                    Log.Warn("Hook is not installed, no need to perform unhooking");
                    return;
                }
                Log.Debug("Unhooking...");
                var currentThreadId = ThreadNativeMethods.GetCurrentThreadId();
                var currentManagedThreadId = $"{Thread.CurrentThread.ManagedThreadId}-{Thread.CurrentThread.Name}";
                if (currentThreadId != NativeThreadId)
                {
                    throw new InvalidOperationException($"Hook is disposed not on the same thread it was created on, initial: {NativeThreadId} ({ManagedThreadId}), current: {currentThreadId} ({currentManagedThreadId})");
                }
                Handle.Dispose();
                Log.Debug("Unhooked");
            }).AddTo(Anchors);
            Disposable.Create(() => Log.Debug("Disposed hook")).AddTo(Anchors);
        }

        public long HookId { get; } = Interlocked.Increment(ref GlobalHookResultId);

        protected IFluentLog Log { get; }

        public HookProcedureHandle Handle { get; protected set; }

        public User32.WindowsHookType HookType { get; }

        public int NativeThreadId { get; }

        public string ManagedThreadId { get; }

        protected static HookProcedureHandle PrepareHandle(IFluentLog log, User32.WindowsHookType hookType, int nativeThreadId, User32.WindowsHookDelegate hookProcedure)
        {
            var baseAddress = hookType switch
            {
                User32.WindowsHookType.WH_MOUSE => IntPtr.Zero,
                User32.WindowsHookType.WH_KEYBOARD => IntPtr.Zero,
                User32.WindowsHookType.WH_JOURNALPLAYBACK => BaseAddress,
                User32.WindowsHookType.WH_MOUSE_LL => BaseAddress,
                User32.WindowsHookType.WH_KEYBOARD_LL => BaseAddress,
                _ => throw new ArgumentOutOfRangeException(nameof(hookType), hookType, $"Unsupported hook type")
            };

            var hookThreadId = baseAddress == IntPtr.Zero ? nativeThreadId : 0;
            var result = User32.SetWindowsHookEx(
                hookType,
                hookProcedure,
                baseAddress,
                hookThreadId);
            
            if (result.IsInvalid)
            {
                log.Warn($"Failed to set new hook with id {hookType}, result: {result}");
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }

            return new HookProcedureHandle(result);
        }

        protected static int CallNextHookEx(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return User32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public override string ToString()
        {
            return $"HookResult#{HookId}({HookType}){(Anchors.IsDisposed ? " Disposed" : string.Empty)} @ thread {NativeThreadId}(managed {ManagedThreadId})";
        }
    }
}