// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsHook.Implementation;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace WindowsHook.WinApi
{
    internal static class HookHelper
    {
        private static readonly IFluentLog Log = typeof(HookHelper).PrepareLogger();

        private static readonly IntPtr BaseAddress;

        static HookHelper()
        {
            Log.Debug("Initializing HookHelper");
            BaseAddress = Process.GetCurrentProcess().MainModule.BaseAddress;
            Log.Debug($"Application base address: {BaseAddress.ToHexadecimal()}");
        }

        public static HookResult HookAppMouse(Callback callback)
        {
            return SetHook(HookIds.WH_MOUSE,IntPtr.Zero,  callback);
        }

        public static HookResult HookAppKeyboard(Callback callback)
        {
            return SetHook(HookIds.WH_KEYBOARD, IntPtr.Zero,  callback);
        }

        public static HookResult HookGlobalMouse(Callback callback)
        {
            return SetHook(HookIds.WH_MOUSE_LL, BaseAddress, callback);
        }

        public static HookResult HookGlobalKeyboard(Callback callback)
        {
            return SetHook(HookIds.WH_KEYBOARD_LL, BaseAddress, callback);
        }

        private static HookResult SetHook(int hookId, IntPtr baseAddress, Callback callback)
        {
            Log.Debug($"Creating a new hook with id {hookId}");
            HookProcedure newHook = (code, param, lParam) => HandleHook(code, param, lParam, callback);
            Log.Debug($"Setting new hook with id {hookId}");
            var hookHandle = HookNativeMethods.SetWindowsHookEx(
                hookId,
                newHook,
                baseAddress,
                baseAddress == IntPtr.Zero ? ThreadNativeMethods.GetCurrentThreadId() : 0);

            if (hookHandle.IsInvalid)
            {
                Log.Warn($"Failed to set new hook with id {hookId}, result: {hookHandle}");
                ThrowLastUnmanagedErrorAsException();
            }
            var result = new HookResult(hookHandle, newHook);
            Log.Debug($"Successfully set new hook with id {hookId}, hook result: {result}");
            return result;
        }

        private static IntPtr HandleHook(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        {
            var passThrough = nCode != 0;
            if (passThrough)
                return CallNextHookEx(nCode, wParam, lParam);

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

        private static void ThrowLastUnmanagedErrorAsException()
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode);
        }
    }
}