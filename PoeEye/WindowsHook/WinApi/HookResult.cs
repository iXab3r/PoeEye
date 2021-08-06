// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace WindowsHook.WinApi
{
    internal class HookResult : IDisposable
    {
        private static long GlobalHookResultId = 0;
        
        private readonly long hookResultId = Interlocked.Increment(ref GlobalHookResultId);
        
        public HookResult(HookProcedureHandle handle, HookProcedure procedure)
        {
            Log = typeof(HookHelper).PrepareLogger().WithSuffix(this.ToString);
            Handle = handle;
            Procedure = procedure;
            NativeThreadId = ThreadNativeMethods.GetCurrentThreadId();
            ManagedThread = Thread.CurrentThread.Name;
        }
        
        private IFluentLog Log { get; }

        public HookProcedureHandle Handle { get; }

        public HookProcedure Procedure { get; }
        
        public int NativeThreadId { get; }
        
        public string ManagedThread { get; }

        public void Dispose()
        {
            Log.Debug("Disposing...");
            Handle.Dispose();
            Log.Debug("Disposed");
        }

        ~HookResult()
        {
            Log.Debug("Finalizer called...");
        }

        public override string ToString()
        {
            return $"HookResult#{hookResultId} @ {NativeThreadId}(managed {ManagedThread})";
        }
    }
}