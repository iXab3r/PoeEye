// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace WindowsHook.WinApi
{
    public sealed class HookProcedureHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static readonly IFluentLog Log = typeof(HookProcedureHandle).PrepareLogger();
        private static volatile bool ApplicationIsClosing;
        private readonly User32.SafeHookHandle hookHandle;

        static HookProcedureHandle()
        {
            Application.ApplicationExit += (sender, e) =>
            {
                ApplicationIsClosing = true;
            };
        }

        public HookProcedureHandle(User32.SafeHookHandle hookHandle)
            : base(true)
        {
            Log.Debug($"Creating hook handle for {hookHandle}, isClosed: {hookHandle.IsClosed}, isInvalid: {hookHandle.IsInvalid}");
            this.hookHandle = hookHandle;
            if (hookHandle.IsClosed || hookHandle.IsInvalid)
            {
                return;
            }
            SetHandle(hookHandle.DangerousGetHandle());
        }

        protected override bool ReleaseHandle()
        {
            //NOTE Calling Unhook during processexit causes delay
            if (ApplicationIsClosing)
            {
                Log.Debug("Application is closing - do not need to release the hook");
                return true;
            }

            if (hookHandle.IsInvalid)
            {
                Log.Debug($"Hook is invalid");
                return false;
            }
            
            if (hookHandle.IsClosed)
            {
                Log.Debug($"Hook is already disposed");
                return true;
            }
            
            Log.Debug("Releasing hook...");
            //The hook procedure can be in the state of being called by another thread even after UnhookWindowsHookEx returns.
            //If the hook procedure is not being called concurrently, the hook procedure is removed immediately before UnhookWindowsHookEx returns.
            hookHandle.Dispose();
            if (hookHandle.IsClosed)
            {
                Log.Debug($"Successfully removed hook");
                return true;
            }
            Log.Warn($"Failed to remove hook"); // throw here ? 
            return false;
        }
    }
}