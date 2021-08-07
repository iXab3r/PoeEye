// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.Implementation;

namespace WindowsHook.WinApi
{
    internal static class HookHelper
    {
        private static readonly IFluentLog Log = typeof(HookHelper).PrepareLogger();

        public static HookResult HookAppMouse(Callback callback)
        {
            return SetHook(WindowsHookType.WH_MOUSE, callback);
        }

        public static HookResult HookAppKeyboard(Callback callback)
        {
            return SetHook(WindowsHookType.WH_KEYBOARD, callback);
        }

        public static HookResult HookGlobalMouse(Callback callback)
        {
            return SetHook(WindowsHookType.WH_MOUSE_LL, callback);
        }

        public static HookResult HookGlobalKeyboard(Callback callback)
        {
            return SetHook(WindowsHookType.WH_KEYBOARD_LL, callback);
        }

        private static HookResult SetHook(WindowsHookType hookType, Callback callback)
        {
            Log.Debug($"Creating a new hook with id {hookType}");
            var hookHandle = new HookResult(hookType, callback);
            Log.Debug($"Successfully set new hook with id {hookType}, result: {hookHandle}");
            return hookHandle;
        }
    }
}