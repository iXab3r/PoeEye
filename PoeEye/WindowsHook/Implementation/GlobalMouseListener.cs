// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Drawing;
using System.Windows.Forms;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

internal class GlobalMouseListener : MouseListener
{
    public GlobalMouseListener()
        : base(HookHelper.HookGlobalMouse)
    {
    }

    protected override MouseEventExtArgs GetEventArgs(WinHookCallbackData data)
    {
        return MouseEventExtArgs.FromRawDataGlobal(data);
    }
}