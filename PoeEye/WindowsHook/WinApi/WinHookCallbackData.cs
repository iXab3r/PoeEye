// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;

namespace WindowsHook.WinApi
{
    public readonly struct WinHookCallbackData
    {
        public WinHookCallbackData(int code, IntPtr wParam, IntPtr lParam)
        {
            Code = code;
            WParam = wParam;
            LParam = lParam;
        }

        public int Code { get; }
        
        public IntPtr WParam { get; }

        public IntPtr LParam { get; }
    }
}