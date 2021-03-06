﻿using System.Windows.Forms;
using System.Windows.Input;

namespace PoeShared.Scaffolding
{
    public static class KeyEventArgsExtensions
    {
        public static ModifierKeys ToModifiers(this Keys keys)
        {
            var result = ModifierKeys.None;
            if (keys.HasFlag(Keys.Control))
            {
                result |= ModifierKeys.Control;
            }

            if (keys.HasFlag(Keys.Shift))
            {
                result |= ModifierKeys.Shift;
            }

            if (keys.HasFlag(Keys.Alt))
            {
                result |= ModifierKeys.Alt;
            }

            if (keys.HasFlag(Keys.LWin) || keys.HasFlag(Keys.RWin))
            {
                result |= ModifierKeys.Windows;
            }

            return result;
        }

        public static Key ToInputKey(this Keys keys)
        {
            return KeyInterop.KeyFromVirtualKey((int) keys.RemoveFlag(Keys.Control, Keys.Alt, Keys.Shift));
        }
    }
}