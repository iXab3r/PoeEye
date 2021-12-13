// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.Implementation;

namespace WindowsHook.WinApi
{
    public static class HookHelper
    {
        private static readonly IFluentLog Log = typeof(HookHelper).PrepareLogger();

        public static HookResult HookAppMouse(WinHookCallback callback)
        {
            return SetHook(User32.WindowsHookType.WH_MOUSE, callback);
        }

        public static HookResult HookAppKeyboard(WinHookCallback callback)
        {
            return SetHook(User32.WindowsHookType.WH_KEYBOARD, callback);
        }

        public static HookResult HookGlobalMouse(WinHookCallback callback)
        {
            return SetHook(User32.WindowsHookType.WH_MOUSE_LL, callback);
        }

        public static HookResult HookGlobalKeyboard(WinHookCallback callback)
        {
            return SetHook(User32.WindowsHookType.WH_KEYBOARD_LL, callback);
        }

        public static HookResult SetHook(User32.WindowsHookType hookType, WinHookCallback callback)
        {
            Log.Debug(() => $"Creating a new hook with id {hookType}");
            var hookHandle = new HookResultWithCallback(hookType, callback);
            Log.Debug(() => $"Successfully set new hook with id {hookType}, result: {hookHandle}");
            return hookHandle;
        }
        
        public static HookResult SetHook(User32.WindowsHookType hookType, User32.WindowsHookDelegate hookProcedure)
        {
            Log.Debug(() => $"Creating a new hook with id {hookType}");
            var hookHandle = new HookResultWithProcedure(hookType, hookProcedure);
            Log.Debug(() => $"Successfully set new hook with id {hookType}, result: {hookHandle}");
            return hookHandle;
        }
    }
}