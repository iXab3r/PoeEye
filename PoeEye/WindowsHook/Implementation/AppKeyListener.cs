// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Collections.Generic;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

internal class AppKeyListener : KeyListener
{
    public AppKeyListener()
        : base(HookHelper.HookAppKeyboard)
    {
    }

    protected override IEnumerable<KeyPressEventArgsExt> GetPressEventArgs(WinHookCallbackData data)
    {
        return KeyPressEventArgsExt.FromRawDataApp(data);
    }

    protected override KeyEventArgsExt GetDownUpEventArgs(WinHookCallbackData data)
    {
        return KeyEventArgsExt.FromRawDataApp(data);
    }
}