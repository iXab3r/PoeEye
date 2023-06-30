// This code is distributed under MIT license.
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Drawing;
using System.Windows.Forms;
using PInvoke;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation;

internal abstract class MouseListener : BaseListener, IMouseEvents
{
    protected MouseListener(Subscribe subscribe)
        : base(subscribe)
    {
        IsReady = true;
    }

    public event EventHandler<MouseEventExtArgs> MouseRaw;
    public event EventHandler<MouseEventExtArgs> MouseMoveExt;
    public event EventHandler<MouseEventExtArgs> MouseDownExt;
    public event EventHandler<MouseEventExtArgs> MouseUpExt;
    public event EventHandler<MouseEventExtArgs> MouseWheelExt;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected override bool Callback(WinHookCallbackData data)
    {
        var e = GetEventArgs(data);
        if (e == null)
        {
            return false;
        }

        MouseRaw?.Invoke(this, e);
        

        /*
        According to the Microsoft documentation, these are the messages that the WH_MOUSE_LL hook procedure can receive:

        WM_LBUTTONDOWN
        WM_LBUTTONUP
        WM_MOUSEMOVE
        WM_MOUSEWHEEL
        WM_MOUSEHWHEEL
        WM_RBUTTONDOWN
        WM_RBUTTONUP
        WM_MBUTTONDOWN
        WM_MBUTTONUP
        WM_XBUTTONDOWN
        WM_XBUTTONUP
         */
        switch (e.Message)
        {
            case User32.WindowMessage.WM_LBUTTONDOWN:
            case User32.WindowMessage.WM_RBUTTONDOWN:
            case User32.WindowMessage.WM_MBUTTONDOWN:
            case User32.WindowMessage.WM_XBUTTONDOWN:
                ProcessDown(ref e);
                break;
            case User32.WindowMessage.WM_LBUTTONUP:
            case User32.WindowMessage.WM_RBUTTONUP:
            case User32.WindowMessage.WM_MBUTTONUP:
            case User32.WindowMessage.WM_XBUTTONUP:
                ProcessUp(ref e);
                break;
            case User32.WindowMessage.WM_MOUSEWHEEL:
            case User32.WindowMessage.WM_MOUSEHWHEEL:
                ProcessWheel(ref e);
                break;
            case User32.WindowMessage.WM_MOUSEMOVE:
                ProcessMove(ref e);
                break;
        }
        
        return !e.Handled;
    }

    protected abstract MouseEventExtArgs GetEventArgs(WinHookCallbackData data);

    protected void ProcessWheel(ref MouseEventExtArgs e)
    {
        OnWheelExt(e);
    }

    protected virtual void ProcessDown(ref MouseEventExtArgs e)
    {
        OnDownExt(e);
    }

    protected virtual void ProcessUp(ref MouseEventExtArgs e)
    {
        OnUpExt(e);
    }

    private void ProcessMove(ref MouseEventExtArgs e)
    {
        OnMoveExt(e);
    }

    protected void OnMoveExt(MouseEventExtArgs e)
    {
        var handler = MouseMoveExt;
        handler?.Invoke(this, e);
    }

    protected void OnDownExt(MouseEventExtArgs e)
    {
        var handler = MouseDownExt;
        handler?.Invoke(this, e);
    }

    protected void OnUpExt(MouseEventExtArgs e)
    {
        var handler = MouseUpExt;
        handler?.Invoke(this, e);
    }

    protected void OnWheelExt(MouseEventExtArgs e)
    {
        var handler = MouseWheelExt;
        handler?.Invoke(this, e);
    }
}