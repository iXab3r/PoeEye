// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Windows.Forms;

namespace WindowsHook
{
    /// <summary>
    ///     Provides all mouse events.
    /// </summary>
    public interface IMouseEvents
    {
        /// <summary>
        ///   Occurs when any mouse event arrives before all other events are invoked
        /// </summary>
        event EventHandler<MouseEventExtArgs> MouseRaw;
        
        /// <summary>
        ///     Occurs when the mouse pointer is moved.
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse movement in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseMoveExt;

        /// <summary>
        ///     Occurs when the mouse a mouse button is pressed.
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse click in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseDownExt;

        /// <summary>
        ///     Occurs when a mouse button is released.
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse click in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseUpExt;

        /// <summary>
        ///     Occurs when the mouse wheel moves.
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse wheel moves in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseWheelExt;

        /// <summary>
        ///     Occurs when a drag event has started (left button held down whilst moving more than the system drag threshold).
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse movement in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseDragStartedExt;

        /// <summary>
        ///     Occurs when a drag event has completed.
        /// </summary>
        /// <remarks>
        ///     This event provides extended arguments of type <see cref="MouseEventArgs" /> enabling you to
        ///     suppress further processing of mouse movement in other applications.
        /// </remarks>
        event EventHandler<MouseEventExtArgs> MouseDragFinishedExt;
    }
}