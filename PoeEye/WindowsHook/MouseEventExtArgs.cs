// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsHook.WinApi;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace WindowsHook
{
    /// <summary>
    ///     Provides extended data for the MouseClickExt and MouseMoveExt events.
    /// </summary>
    public sealed class MouseEventExtArgs : MouseEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MouseEventExtArgs" /> class.
        /// </summary>
        /// <param name="buttons">One of the MouseButtons values indicating which mouse button was pressed.</param>
        /// <param name="clicks">The number of times a mouse button was pressed.</param>
        /// <param name="point">The x and y coordinate of a mouse click, in pixels.</param>
        /// <param name="delta">A signed count of the number of detents the wheel has rotated.</param>
        /// <param name="timestamp">The system tick count when the event occurred.</param>
        /// <param name="isMouseButtonDown">True if event signals mouse button down.</param>
        /// <param name="isMouseButtonUp">True if event signals mouse button up.</param>
        /// <param name="modifiers">CTRL, ALT, SHIFT, etc</param>
        public MouseEventExtArgs(MouseButtons buttons, int clicks, Point point, int delta, int timestamp,
            bool isMouseButtonDown, bool isMouseButtonUp, Keys modifiers, bool isInjected)
            : base(buttons, clicks, point.X, point.Y, delta)
        {
            IsMouseButtonDown = isMouseButtonDown;
            IsMouseButtonUp = isMouseButtonUp;
            Timestamp = timestamp;
            Modifiers = modifiers;
            IsInjected = isInjected;
        }

        public MouseEventExtArgs(MouseEventExtArgs args, MouseButtons buttons) : this(buttons, args.Clicks, args.Point, args.Delta, args.Timestamp, args.IsMouseButtonDown, args.IsMouseButtonUp, args.Modifiers, args.IsInjected)
        {
            Handled = args.Handled;
        }

        public MouseEventExtArgs(MouseButtons buttons, int clicks, int positionX, int positionY, int timestamp) : base(buttons, clicks, positionX, positionY, timestamp)
        {
        }

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
        ///     True if event signals a click. False if it was only a move or wheel scroll.
        /// </summary>
        public bool Clicked => Clicks > 0;

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

        /// <summary>
        /// </summary>
        internal Point Point => new Point(X, Y);
        
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
            return marshalledMouseStruct == null ? default : FromRawDataUniversal(wParam, ((AppMouseStruct)marshalledMouseStruct).ToMouseStruct());
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
            return marshalledMouseStruct == null ? default : FromRawDataUniversal(wParam, ((MouseStruct)marshalledMouseStruct));
        }

        /// <summary>
        ///     Creates <see cref="MouseEventExtArgs" /> from relevant mouse data.
        /// </summary>
        /// <param name="wParam">First Windows Message parameter.</param>
        /// <param name="mouseInfo">A MouseStruct containing information from which to construct MouseEventExtArgs.</param>
        /// <returns>A new MouseEventExtArgs object.</returns>
        private static MouseEventExtArgs FromRawDataUniversal(IntPtr wParam, MouseStruct mouseInfo)
        {
            var button = MouseButtons.None;
            short mouseDelta = 0;
            var clickCount = 0;

            var isMouseButtonDown = false;
            var isMouseButtonUp = false;


            switch ((long)wParam)
            {
                case Messages.WM_LBUTTONDOWN:
                    isMouseButtonDown = true;
                    button = MouseButtons.Left;
                    clickCount = 1;
                    break;
                case Messages.WM_LBUTTONUP:
                    isMouseButtonUp = true;
                    button = MouseButtons.Left;
                    clickCount = 1;
                    break;
                case Messages.WM_LBUTTONDBLCLK:
                    isMouseButtonDown = true;
                    button = MouseButtons.Left;
                    clickCount = 2;
                    break;
                case Messages.WM_RBUTTONDOWN:
                    isMouseButtonDown = true;
                    button = MouseButtons.Right;
                    clickCount = 1;
                    break;
                case Messages.WM_RBUTTONUP:
                    isMouseButtonUp = true;
                    button = MouseButtons.Right;
                    clickCount = 1;
                    break;
                case Messages.WM_RBUTTONDBLCLK:
                    isMouseButtonDown = true;
                    button = MouseButtons.Right;
                    clickCount = 2;
                    break;
                case Messages.WM_MBUTTONDOWN:
                    isMouseButtonDown = true;
                    button = MouseButtons.Middle;
                    clickCount = 1;
                    break;
                case Messages.WM_MBUTTONUP:
                    isMouseButtonUp = true;
                    button = MouseButtons.Middle;
                    clickCount = 1;
                    break;
                case Messages.WM_MBUTTONDBLCLK:
                    isMouseButtonDown = true;
                    button = MouseButtons.Middle;
                    clickCount = 2;
                    break;
                case Messages.WM_MOUSEWHEEL:
                    mouseDelta = mouseInfo.MouseData;
                    break;
                case Messages.WM_XBUTTONDOWN:
                    button = mouseInfo.MouseData == 1
                        ? MouseButtons.XButton1
                        : MouseButtons.XButton2;
                    isMouseButtonDown = true;
                    clickCount = 1;
                    break;

                case Messages.WM_XBUTTONUP:
                    button = mouseInfo.MouseData == 1
                        ? MouseButtons.XButton1
                        : MouseButtons.XButton2;
                    isMouseButtonUp = true;
                    clickCount = 1;
                    break;

                case Messages.WM_XBUTTONDBLCLK:
                    isMouseButtonDown = true;
                    button = mouseInfo.MouseData == 1
                        ? MouseButtons.XButton1
                        : MouseButtons.XButton2;
                    clickCount = 2;
                    break;

                case Messages.WM_MOUSEHWHEEL:
                    mouseDelta = mouseInfo.MouseData;
                    break;
            }
            
            var isInjected = mouseInfo.Flags.HasFlag(MouseHookLowLevelFlags.LLMHF_INJECTED) || mouseInfo.Flags.HasFlag(MouseHookLowLevelFlags.LLMHF_LOWER_IL_INJECTED);

            var modifiers = GetCurrentModifierKeys();
            var e = new MouseEventExtArgs(
                button,
                clickCount,
                mouseInfo.Point,
                mouseDelta,
                mouseInfo.Timestamp,
                isMouseButtonDown: isMouseButtonDown,
                isMouseButtonUp: isMouseButtonUp,
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

        internal MouseEventExtArgs ToDoubleClickEventArgs()
        {
            return new MouseEventExtArgs(Button, 2, Point, Delta, Timestamp, IsMouseButtonDown, IsMouseButtonUp, Modifiers, IsInjected);
        }

        public override string ToString()
        {
            return $"{nameof(Location)}: {Location}, {nameof(Button)}: {Button}, {nameof(Delta)}: {Delta}, {nameof(Modifiers)}: {Modifiers}, {nameof(Handled)}: {Handled}, {nameof(IsInjected)}: {IsInjected}";
        }
    }
}