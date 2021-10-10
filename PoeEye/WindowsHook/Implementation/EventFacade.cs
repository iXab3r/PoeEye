// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Reactive.Disposables;
using System.Text;
using System.Windows.Forms;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace WindowsHook.Implementation
{
    internal abstract class EventFacade : DisposableReactiveObject, IKeyboardMouseEvents
    {
        private static readonly IFluentLog SharedLog = typeof(EventFacade).PrepareLogger();

        private readonly Lazy<KeyListener> keyListener;
        private readonly Lazy<MouseListener> mouseListener;

        protected EventFacade()
        {
            Log = SharedLog.WithSuffix(ToString);
            keyListener = new Lazy<KeyListener>(() =>
            {
                if (Anchors.IsDisposed)
                {
                    throw new ObjectDisposedException("Facade");
                }

                return CreateKeyListener().AddTo(Anchors);
            });
            mouseListener = new Lazy<MouseListener>(() =>
            {
                if (Anchors.IsDisposed)
                {
                    throw new ObjectDisposedException("Facade");
                }
                return CreateMouseListener().AddTo(Anchors);
            });
            Log.Debug("Created event facade");
            Disposable.Create(() => Log.Debug("Disposed event facade")).AddTo(Anchors);
        }
        
        protected IFluentLog Log { get; }

        event KeyEventHandler IKeyboardEvents.KeyRaw
        {
            add => keyListener.Value.KeyRaw += value;
            remove => keyListener.Value.KeyRaw += value;
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

        event EventHandler<MouseEventExtArgs> IMouseEvents.MouseRaw
        {
            add => mouseListener.Value.MouseRaw += value;
            remove => mouseListener.Value.MouseRaw += value;
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

        protected abstract MouseListener CreateMouseListener();
        protected abstract KeyListener CreateKeyListener();

        public override string ToString()
        {
            var result = new StringBuilder("Facade");

            if (Anchors.IsDisposed)
            {
                result.Append(" Disposed");
            }

            if (mouseListener.IsValueCreated)
            {
                result.Append($" Mouse: {mouseListener.Value}");
            }

            if (keyListener.IsValueCreated)
            {
                result.Append($" Keyboard: {keyListener.Value}");
            }

            return result.ToString();
        }
    }
}