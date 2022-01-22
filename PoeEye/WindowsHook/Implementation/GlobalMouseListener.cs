// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Drawing;
using System.Windows.Forms;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

internal class GlobalMouseListener : MouseListener
{
    private readonly int m_SystemDoubleClickTime;
    private MouseButtons m_PreviousClicked;
    private Point m_PreviousClickedPosition;
    private int m_PreviousClickedTime;

    public GlobalMouseListener()
        : base(HookHelper.HookGlobalMouse)
    {
        m_SystemDoubleClickTime = MouseNativeMethods.GetDoubleClickTime();
    }

    protected override void ProcessDown(ref MouseEventExtArgs e)
    {
        if (IsDoubleClick(e))
        {
            e = e.ToDoubleClickEventArgs();
        }

        base.ProcessDown(ref e);
    }

    protected override void ProcessUp(ref MouseEventExtArgs e)
    {
        base.ProcessUp(ref e);
        switch (e.Clicks)
        {
            case 2:
                StopDoubleClickWaiting();
                break;
            case 1:
                StartDoubleClickWaiting(e);
                break;
        }
    }

    private void StartDoubleClickWaiting(MouseEventExtArgs e)
    {
        m_PreviousClicked = e.Button;
        m_PreviousClickedTime = e.Timestamp;
        m_PreviousClickedPosition = e.Point;
    }

    private void StopDoubleClickWaiting()
    {
        m_PreviousClicked = MouseButtons.None;
        m_PreviousClickedTime = 0;
        m_PreviousClickedPosition = new Point(0, 0);
    }

    private bool IsDoubleClick(MouseEventExtArgs e)
    {
        return
            e.Button == m_PreviousClicked &&
            e.Point == m_PreviousClickedPosition && // Click-move-click exception, see Patch 11222
            e.Timestamp - m_PreviousClickedTime <= m_SystemDoubleClickTime;
    }

    protected override MouseEventExtArgs GetEventArgs(WinHookCallbackData data)
    {
        return MouseEventExtArgs.FromRawDataGlobal(data);
    }
}