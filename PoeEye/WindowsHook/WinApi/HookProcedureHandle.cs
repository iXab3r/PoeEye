// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace WindowsHook.WinApi
{
    internal class HookProcedureHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static readonly IFluentLog Log = typeof(HookProcedureHandle).PrepareLogger();
        private static volatile bool ApplicationIsClosing;

        static HookProcedureHandle()
        {
            Application.ApplicationExit += (sender, e) =>
            {
                ApplicationIsClosing = true;
            };
        }

        public HookProcedureHandle()
            : base(true)
        {
        }
        
        protected override bool ReleaseHandle()
        {
            //NOTE Calling Unhook during processexit causes deley
            if (ApplicationIsClosing)
            {
                Log.Debug("Application is closing - do not need to release the hook");
                return true;
            }

            Log.Debug("Releasing hook...");
            //The hook procedure can be in the state of being called by another thread even after UnhookWindowsHookEx returns.
            //If the hook procedure is not being called concurrently, the hook procedure is removed immediately before UnhookWindowsHookEx returns.
            var result = HookNativeMethods.UnhookWindowsHookEx(handle);
            if (result != 0)
            {
                Log.Debug($"Successfully removed hook");
                return true;
            }
            Log.Warn($"Failed to remove hook"); // throw here ? 
            return false;
        }
    }
}