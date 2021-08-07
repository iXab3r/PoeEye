// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation
{
    internal abstract class BaseListener : IDisposable
    {
        protected BaseListener(Subscribe subscribe)
        {
            Handle = subscribe(CallbackHook);
        }

        protected HookResult Handle { get; set; }

        protected bool IsReady { get; init; }

        public void Dispose()
        {
            Handle.Dispose();
        }

        private bool CallbackHook(CallbackData data)
        {
            if (!IsReady)
            {
                return false;
            }

            return Callback(data);
        }

        protected abstract bool Callback(CallbackData data);
    }
}