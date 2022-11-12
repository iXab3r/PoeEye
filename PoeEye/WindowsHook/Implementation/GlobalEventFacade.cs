// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using PoeShared.Scaffolding;

namespace WindowsHook.Implementation;

internal class GlobalEventFacade : EventFacade
{
    protected override MouseListener CreateMouseListener()
    {
        return new GlobalMouseListener();
    }

    protected override KeyListener CreateKeyListener()
    {
        return new GlobalKeyListener();
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("Global");
    }
}