// This code is distributed under MIT license.
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation
{
    internal abstract class MouseListener : BaseListener, IMouseEvents
    {
        private readonly ButtonSet m_DoubleDown;
        private readonly ButtonSet m_SingleDown;
        private readonly Point m_UninitialisedPoint = new(-1, -1);
        private readonly int m_xDragThreshold;
        private readonly int m_yDragThreshold;
        private Point m_DragStartPosition;

        private bool m_IsDragging;

        private Point m_PreviousPosition;

        protected MouseListener(Subscribe subscribe)
            : base(subscribe)
        {
            m_xDragThreshold = NativeMethods.GetXDragThreshold();
            m_yDragThreshold = NativeMethods.GetYDragThreshold();
            m_IsDragging = false;

            m_PreviousPosition = m_UninitialisedPoint;
            m_DragStartPosition = m_UninitialisedPoint;

            m_DoubleDown = new ButtonSet();
            m_SingleDown = new ButtonSet();
            IsReady = true;
        }

        public event EventHandler<MouseEventExtArgs> MouseRaw;
        public event EventHandler<MouseEventExtArgs> MouseMoveExt;
        public event EventHandler<MouseEventExtArgs> MouseDownExt;
        public event EventHandler<MouseEventExtArgs> MouseUpExt;
        public event EventHandler<MouseEventExtArgs> MouseWheelExt;
        public event EventHandler<MouseEventExtArgs> MouseDragStartedExt;
        public event EventHandler<MouseEventExtArgs> MouseDragFinishedExt;

        protected override bool Callback(WinHookCallbackData data)
        {
            var e = GetEventArgs(data);
            if (e == null)
            {
                return false;
            }

            MouseRaw?.Invoke(this, e);

            if (e.IsMouseButtonDown)
            {
                ProcessDown(ref e);
            }

            if (e.IsMouseButtonUp)
            {
                ProcessUp(ref e);
            }

            if (e.WheelScrolled)
            {
                ProcessWheel(ref e);
            }

            var hasMoved = HasMoved(e.Point);
            if (hasMoved)
            {
                ProcessMove(ref e);
            }

            ProcessDrag(ref e);

            return !e.Handled;
        }

        private MouseEventExtArgs EnrichWithButtons(MouseEventExtArgs args)
        {
            var buttons = args.Button | m_SingleDown.Values;
            return new MouseEventExtArgs(args, buttons);
        }

        protected abstract MouseEventExtArgs GetEventArgs(WinHookCallbackData data);

        protected virtual void ProcessWheel(ref MouseEventExtArgs e)
        {
            OnWheelExt(e);
        }

        protected virtual void ProcessDown(ref MouseEventExtArgs e)
        {
            OnDownExt(e);
            if (e.Handled)
            {
                return;
            }

            switch (e.Clicks)
            {
                case 2:
                    m_DoubleDown.Add(e.Button);
                    break;
                case 1:
                    m_SingleDown.Add(e.Button);
                    break;
            }
        }

        protected virtual void ProcessUp(ref MouseEventExtArgs e)
        {
            OnUpExt(e);
            if (e.Handled)
            {
                return;
            }

            if (m_SingleDown.Contains(e.Button))
            {
                m_SingleDown.Remove(e.Button);
            }

            if (m_DoubleDown.Contains(e.Button))
            {
                e = e.ToDoubleClickEventArgs();
                m_DoubleDown.Remove(e.Button);
            }
        }

        private void ProcessMove(ref MouseEventExtArgs e)
        {
            e = EnrichWithButtons(e);
            m_PreviousPosition = e.Point;
            OnMoveExt(e);
        }

        private void ProcessDrag(ref MouseEventExtArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if (m_SingleDown.Contains(MouseButtons.Left))
            {
                if (m_DragStartPosition.Equals(m_UninitialisedPoint))
                {
                    m_DragStartPosition = e.Point;
                }

                ProcessDragStarted(ref e);
            }
            else
            {
                m_DragStartPosition = m_UninitialisedPoint;
                ProcessDragFinished(ref e);
            }
        }

        private void ProcessDragStarted(ref MouseEventExtArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if (m_IsDragging)
            {
                return;
            }

            var isXDragging = Math.Abs(e.Point.X - m_DragStartPosition.X) > m_xDragThreshold;
            var isYDragging = Math.Abs(e.Point.Y - m_DragStartPosition.Y) > m_yDragThreshold;
            m_IsDragging = isXDragging || isYDragging;

            if (m_IsDragging)
            {
                OnDragStartedExt(e);
            }
        }

        private void ProcessDragFinished(ref MouseEventExtArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if (m_IsDragging)
            {
                OnDragFinishedExt(e);
                m_IsDragging = false;
            }
        }

        private bool HasMoved(Point actualPoint)
        {
            return m_PreviousPosition != actualPoint;
        }

        protected virtual void OnMoveExt(MouseEventExtArgs e)
        {
            var handler = MouseMoveExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDownExt(MouseEventExtArgs e)
        {
            var handler = MouseDownExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnUpExt(MouseEventExtArgs e)
        {
            var handler = MouseUpExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnWheelExt(MouseEventExtArgs e)
        {
            var handler = MouseWheelExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDragStartedExt(MouseEventExtArgs e)
        {
            var handler = MouseDragStartedExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDragFinishedExt(MouseEventExtArgs e)
        {
            var handler = MouseDragFinishedExt;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}