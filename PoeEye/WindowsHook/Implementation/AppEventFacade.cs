// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using PoeShared.Scaffolding;

namespace WindowsHook.Implementation;

internal class AppEventFacade : EventFacade
{
    protected override MouseListener CreateMouseListener()
    {
        return new AppMouseListener();
    }

    protected override KeyListener CreateKeyListener()
    {
        return new AppKeyListener();
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("App");
    }
}