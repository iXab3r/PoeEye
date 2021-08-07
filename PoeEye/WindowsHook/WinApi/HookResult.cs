// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.Implementation;

namespace WindowsHook.WinApi
{
    internal class HookResult : DisposableReactiveObject
    {
        private static readonly IFluentLog SharedLog = typeof(HookResult).PrepareLogger();
        private static readonly IntPtr BaseAddress;
        private static long GlobalHookResultId;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly HookProcedure hookProcedure;

        static HookResult()
        {
            SharedLog.Debug("Initializing HookHelper");
            var process = Process.GetCurrentProcess();
            var mainModule = process.MainModule ?? throw new NotSupportedException($"Failed to get MainModule in {process}");
            BaseAddress = mainModule.BaseAddress;
            SharedLog.Debug($"Application base address: {BaseAddress.ToHexadecimal()}");
        }

        public HookResult(WindowsHookType hookType, Callback callback)
        {
            Log = SharedLog.WithSuffix(ToString);

            HookType = hookType;
            Callback = callback;
            NativeThreadId = ThreadNativeMethods.GetCurrentThreadId();
            ManagedThreadId = $"{Thread.CurrentThread.ManagedThreadId}-{Thread.CurrentThread.Name}";
            hookProcedure = HandleHookCallback;
            Log.Debug($"Initializing new hook of type {hookType}");

            var baseAddress = hookType switch
            {
                WindowsHookType.WH_MOUSE => IntPtr.Zero,
                WindowsHookType.WH_KEYBOARD => IntPtr.Zero,
                WindowsHookType.WH_MOUSE_LL => BaseAddress,
                WindowsHookType.WH_KEYBOARD_LL => BaseAddress,
                _ => throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null)
            };
            
            Handle = HookNativeMethods.SetWindowsHookEx(
                (int)hookType,
                hookProcedure,
                baseAddress,
                baseAddress == IntPtr.Zero ? NativeThreadId : 0).AddTo(Anchors);

            if (!Handle.IsInvalid)
            {
                return;
            }

            Log.Warn($"Failed to set new hook with id {hookType}, result: {Handle}");
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode);
        }

        public long HookId { get; } = Interlocked.Increment(ref GlobalHookResultId);

        private IFluentLog Log { get; }

        public HookProcedureHandle Handle { get; }

        public WindowsHookType HookType { get; }

        public Callback Callback { get; }

        public int NativeThreadId { get; }
        
        public string ManagedThreadId { get; }

        private IntPtr HandleHookCallback(int ncode, IntPtr wparam, IntPtr lparam)
        {
            return HandleHook(ncode, wparam, lparam, Callback);
        }

        private static IntPtr HandleHook(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        {
            var passThrough = nCode != 0;
            if (passThrough)
            {
                return CallNextHookEx(nCode, wParam, lParam);
            }

            var callbackData = new CallbackData(wParam, lParam);
            var continueProcessing = callback(callbackData);

            if (!continueProcessing)
            {
                return new IntPtr(-1);
            }

            return CallNextHookEx(nCode, wParam, lParam);
        }

        private static IntPtr CallNextHookEx(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return HookNativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public override string ToString()
        {
            return $"HookResult#{HookId}({HookType}) @ thread {NativeThreadId}(managed {ManagedThreadId})";
        }
    }
}