// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using PInvoke;
using WindowsHook.WinApi;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace WindowsHook;

/// <summary>
///     Provides extended data for the MouseClickExt and MouseMoveExt events.
/// </summary>
public sealed class MouseEventExtArgs : MouseEventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseEventExtArgs" /> class.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="clicks">The number of times a mouse button was pressed.</param>
    /// <param name="point">The x and y coordinate of a mouse click, in pixels.</param>
    /// <param name="delta">A signed count of the number of detents the wheel has rotated.</param>
    /// <param name="timestamp">The system tick count when the event occurred.</param>
    /// <param name="modifiers">CTRL, ALT, SHIFT, etc</param>
    /// <param name="isInjected"></param>
    public MouseEventExtArgs(User32.WindowMessage message, Point point, int delta, int timestamp, Keys modifiers, bool isInjected)
        : this(message, default, point, delta, timestamp, modifiers, isInjected)
    {
        Message = message;
        Timestamp = timestamp;
        Modifiers = modifiers;
        IsInjected = isInjected;
        IsMouseButtonDown = Message is User32.WindowMessage.WM_LBUTTONDOWN or User32.WindowMessage.WM_RBUTTONDOWN or User32.WindowMessage.WM_XBUTTONDOWN;
        IsMouseButtonUp = Message is User32.WindowMessage.WM_RBUTTONUP or User32.WindowMessage.WM_LBUTTONUP or User32.WindowMessage.WM_XBUTTONUP;
    }
    
    private MouseEventExtArgs(User32.WindowMessage message, MouseStruct mouseStruct, Point point, int delta, int timestamp, Keys modifiers, bool isInjected)
        : base(ToMouseButtons(message, mouseStruct), ToMouseClicks(message), point.X, point.Y, delta)
    {
        Message = message;
        Timestamp = timestamp;
        Modifiers = modifiers;
        IsInjected = isInjected;
        IsMouseButtonDown = Message is User32.WindowMessage.WM_LBUTTONDOWN or User32.WindowMessage.WM_RBUTTONDOWN or User32.WindowMessage.WM_XBUTTONDOWN;
        IsMouseButtonUp = Message is User32.WindowMessage.WM_RBUTTONUP or User32.WindowMessage.WM_LBUTTONUP or User32.WindowMessage.WM_XBUTTONUP;
    }
    
    public MouseEventExtArgs(MouseButtons buttons, int clicks, int positionX, int positionY, int timestamp) : base(buttons, clicks, positionX, positionY, timestamp)
    {
    }
    
    private static int ToMouseClicks(User32.WindowMessage message)
    {
        return message switch
        {
            User32.WindowMessage.WM_LBUTTONDOWN or User32.WindowMessage.WM_LBUTTONUP => 1,
            User32.WindowMessage.WM_RBUTTONDOWN or User32.WindowMessage.WM_RBUTTONUP => 1,
            User32.WindowMessage.WM_MBUTTONDOWN or User32.WindowMessage.WM_MBUTTONUP => 1,
            User32.WindowMessage.WM_XBUTTONDOWN or User32.WindowMessage.WM_XBUTTONUP => 1,
            _ => 0
        };
    }

    private static MouseButtons ToMouseButtons(User32.WindowMessage message, MouseStruct mouseStruct)
    {
        return message switch
        {
            User32.WindowMessage.WM_LBUTTONDOWN or User32.WindowMessage.WM_LBUTTONUP => MouseButtons.Left,
            User32.WindowMessage.WM_RBUTTONDOWN or User32.WindowMessage.WM_RBUTTONUP => MouseButtons.Right,
            User32.WindowMessage.WM_MBUTTONDOWN or User32.WindowMessage.WM_MBUTTONUP => MouseButtons.Middle,
            User32.WindowMessage.WM_XBUTTONDOWN or User32.WindowMessage.WM_XBUTTONUP => ToMouseButtons(mouseStruct),
            User32.WindowMessage.WM_MOUSEMOVE or User32.WindowMessage.WM_MOUSEWHEEL or User32.WindowMessage.WM_MOUSEHWHEEL => MouseButtons.None,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, $"Unsupported message type: {message}")
        };
    }

    private static MouseButtons ToMouseButtons(MouseStruct mouseStruct)
    {
        return mouseStruct.MouseData switch
        {
            1 => MouseButtons.XButton1,
            2 => MouseButtons.XButton2,
            _ => throw new ArgumentOutOfRangeException(nameof(mouseStruct), mouseStruct, $"Unsupported extra mouse button: {mouseStruct}")
        };
    }
    
    public User32.WindowMessage Message { get; }

    /// <summary>
    ///     Set this property to <b>true</b> inside your event handler to prevent further processing of the event in other
    ///     applications.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     True if event contains information about wheel scroll.
    /// </summary>
    public bool WheelScrolled => Delta != 0;

    /// <summary>
    ///     True if event signals mouse button down.
    /// </summary>
    public bool IsMouseButtonDown { get; } 

    /// <summary>
    ///     True if event signals mouse button up.
    /// </summary>
    public bool IsMouseButtonUp { get; }

    /// <summary>
    ///     The system tick count of when the event occurred.
    /// </summary>
    public int Timestamp { get; }
        
    public Keys Modifiers { get; }
    
    public bool IsInjected { get; }

    internal static MouseEventExtArgs FromRawDataApp(WinHookCallbackData data)
    {
        var wParam = data.WParam;
        var lParam = data.LParam;
        if (lParam == IntPtr.Zero)
        {
            return default;
        }
            
        var marshalledMouseStruct = Marshal.PtrToStructure(lParam, typeof(AppMouseStruct));
        return marshalledMouseStruct == null ? default : FromRawDataUniversal((User32.WindowMessage)wParam, ((AppMouseStruct)marshalledMouseStruct).ToMouseStruct());
    }

    internal static MouseEventExtArgs FromRawDataGlobal(WinHookCallbackData data)
    {
        var wParam = data.WParam;
        var lParam = data.LParam;
        if (lParam == IntPtr.Zero)
        {
            return default;
        }

        var marshalledMouseStruct = Marshal.PtrToStructure(lParam, typeof(MouseStruct));
        return marshalledMouseStruct == null ? default : FromRawDataUniversal((User32.WindowMessage)wParam, (MouseStruct)marshalledMouseStruct);
    }

    /// <summary>
    ///     Creates <see cref="MouseEventExtArgs" /> from relevant mouse data.
    /// </summary>
    /// <param name="wParam">First Windows Message parameter.</param>
    /// <param name="message"></param>
    /// <param name="mouseInfo">A MouseStruct containing information from which to construct MouseEventExtArgs.</param>
    /// <returns>A new MouseEventExtArgs object.</returns>
    private static MouseEventExtArgs FromRawDataUniversal(User32.WindowMessage message, MouseStruct mouseInfo)
    {
        var mouseDelta = message switch
        {
            User32.WindowMessage.WM_MOUSEWHEEL => mouseInfo.MouseData,
            User32.WindowMessage.WM_MOUSEHWHEEL => mouseInfo.MouseData,
            _ => 0
        };

        var isInjected = mouseInfo.Flags.HasFlag(MouseHookLowLevelFlags.LLMHF_INJECTED) || mouseInfo.Flags.HasFlag(MouseHookLowLevelFlags.LLMHF_LOWER_IL_INJECTED);

        Keys modifiers;
        if (message == User32.WindowMessage.WM_MOUSEMOVE)
        {
            //do not capture modifiers on mouse move to avoid lags
            modifiers = Keys.None;
        }
        else
        {
            //FIXME performance hit!
            modifiers = GetCurrentModifierKeys();
        }

        var e = new MouseEventExtArgs(
            message,
            mouseInfo,
            mouseInfo.Point,
            mouseDelta,
            mouseInfo.Timestamp,
            modifiers: modifiers,
            isInjected: isInjected);

        return e;
    }
        
    private static Keys GetCurrentModifierKeys()
    {
        var modifier = Keys.None;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            modifier |= Keys.Control;
        }

        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        {
            modifier |= Keys.Alt;
        }

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            modifier |= Keys.Shift;
        }

        return modifier;
    }

    public override string ToString()
    {
        return $"{nameof(Location)}: {Location}, {nameof(Button)}: {Button} x{Clicks}, {nameof(Delta)}: {Delta}, {nameof(Modifiers)}: {Modifiers}, {nameof(Handled)}: {Handled}, {nameof(IsInjected)}: {IsInjected}, {nameof(Timestamp)}: {Timestamp}";
    }
}