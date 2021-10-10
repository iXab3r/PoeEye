using System.Windows.Forms;
using System.Windows.Input;
using PoeShared.Native;
using PoeShared.UI;
using WindowsHook;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

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

        public static HotkeyGesture ToGesture(this KeyEventArgs args)
        {
            return new HotkeyGesture(args.KeyCode.ToInputKey(), args.Modifiers.ToModifiers());
        }
        
        public static HotkeyGesture ToGesture(this MouseEventArgs args)
        {
            var modifiers = args is MouseEventExtArgs mouseEventExtArgs ? mouseEventExtArgs.Modifiers.ToModifiers() : UnsafeNative.GetCurrentModifierKeys();
            return args.Delta != 0 ? new HotkeyGesture(args.Delta > 0 ? MouseWheelAction.WheelUp : MouseWheelAction.WheelDown, modifiers) : new HotkeyGesture(args.Button, modifiers);
        }
    }
}