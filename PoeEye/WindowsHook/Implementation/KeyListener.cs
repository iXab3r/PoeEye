// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Collections.Generic;
using System.Windows.Forms;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation
{
    internal abstract class KeyListener : BaseListener, IKeyboardEvents
    {
        protected KeyListener(Subscribe subscribe)
            : base(subscribe)
        {
            IsReady = true;
        }

        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;

        public void InvokeKeyDown(KeyEventArgsExt e)
        {
            var handler = KeyDown;
            if (handler == null || e.Handled || !e.IsKeyDown)
            {
                return;
            }

            handler(this, e);
        }

        public void InvokeKeyPress(KeyPressEventArgsExt e)
        {
            var handler = KeyPress;
            if (handler == null || e.Handled || e.IsNonChar)
            {
                return;
            }

            handler(this, e);
        }

        public void InvokeKeyUp(KeyEventArgsExt e)
        {
            var handler = KeyUp;
            if (handler == null || e.Handled || !e.IsKeyUp)
            {
                return;
            }

            handler(this, e);
        }

        protected override bool Callback(CallbackData data)
        {
            var e = GetDownUpEventArgs(data);
            if (e == null)
            {
                return false;
            }

            InvokeKeyDown(e);

            if (KeyPress != null)
            {
                var pressEventArgs = GetPressEventArgs(data);
                foreach (var pressEventArg in pressEventArgs)
                {
                    InvokeKeyPress(pressEventArg);
                }
            }

            InvokeKeyUp(e);

            return !e.Handled;
        }

        protected abstract IEnumerable<KeyPressEventArgsExt> GetPressEventArgs(CallbackData data);
        
        protected abstract KeyEventArgsExt GetDownUpEventArgs(CallbackData data);
    }
}