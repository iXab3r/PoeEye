// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Windows.Forms;

namespace WindowsHook.Implementation
{
    internal abstract class EventFacade : IKeyboardMouseEvents
    {
        private readonly Lazy<KeyListener> keyListener;
        private readonly Lazy<MouseListener> mouseListener;

        protected EventFacade()
        {
            keyListener = new Lazy<KeyListener>(CreateKeyListener);
            mouseListener = new Lazy<MouseListener>(CreateMouseListener);
        }

        public event KeyEventHandler KeyDown
        {
            add => keyListener.Value.KeyDown += value;
            remove => keyListener.Value.KeyDown -= value;
        }

        public event KeyPressEventHandler KeyPress
        {
            add => keyListener.Value.KeyPress += value;
            remove => keyListener.Value.KeyPress -= value;
        }

        public event KeyEventHandler KeyUp
        {
            add => keyListener.Value.KeyUp += value;
            remove => keyListener.Value.KeyUp -= value;
        }

        public event MouseEventHandler MouseMove
        {
            add => mouseListener.Value.MouseMove += value;
            remove => mouseListener.Value.MouseMove -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseMoveExt
        {
            add => mouseListener.Value.MouseMoveExt += value;
            remove => mouseListener.Value.MouseMoveExt -= value;
        }

        public event MouseEventHandler MouseClick
        {
            add => mouseListener.Value.MouseClick += value;
            remove => mouseListener.Value.MouseClick -= value;
        }

        public event MouseEventHandler MouseDown
        {
            add => mouseListener.Value.MouseDown += value;
            remove => mouseListener.Value.MouseDown -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseDownExt
        {
            add => mouseListener.Value.MouseDownExt += value;
            remove => mouseListener.Value.MouseDownExt -= value;
        }

        public event MouseEventHandler MouseUp
        {
            add => mouseListener.Value.MouseUp += value;
            remove => mouseListener.Value.MouseUp -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseUpExt
        {
            add => mouseListener.Value.MouseUpExt += value;
            remove => mouseListener.Value.MouseUpExt -= value;
        }

        public event MouseEventHandler MouseWheel
        {
            add => mouseListener.Value.MouseWheel += value;
            remove => mouseListener.Value.MouseWheel -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseWheelExt
        {
            add => mouseListener.Value.MouseWheelExt += value;
            remove => mouseListener.Value.MouseWheelExt -= value;
        }

        public event MouseEventHandler MouseDoubleClick
        {
            add => mouseListener.Value.MouseDoubleClick += value;
            remove => mouseListener.Value.MouseDoubleClick -= value;
        }

        public event MouseEventHandler MouseDragStarted
        {
            add => mouseListener.Value.MouseDragStarted += value;
            remove => mouseListener.Value.MouseDragStarted -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseDragStartedExt
        {
            add => mouseListener.Value.MouseDragStartedExt += value;
            remove => mouseListener.Value.MouseDragStartedExt -= value;
        }

        public event MouseEventHandler MouseDragFinished
        {
            add => mouseListener.Value.MouseDragFinished += value;
            remove => mouseListener.Value.MouseDragFinished -= value;
        }

        public event EventHandler<MouseEventExtArgs> MouseDragFinishedExt
        {
            add => mouseListener.Value.MouseDragFinishedExt += value;
            remove => mouseListener.Value.MouseDragFinishedExt -= value;
        }

        public void Dispose()
        {
            if (mouseListener.IsValueCreated)
            {
                mouseListener.Value.Dispose();
            }

            if (keyListener.IsValueCreated)
            {
                keyListener.Value.Dispose();
            }
        }

        protected abstract MouseListener CreateMouseListener();
        protected abstract KeyListener CreateKeyListener();
    }
}