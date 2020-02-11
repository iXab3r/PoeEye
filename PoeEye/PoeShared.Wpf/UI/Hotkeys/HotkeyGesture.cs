using System;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using PInvoke;
using PoeShared.Native;

namespace PoeShared.UI.Hotkeys
{
    public class HotkeyGesture : IEquatable<HotkeyGesture>
    {
        public HotkeyGesture()
        {
        }

        public HotkeyGesture(Key key, ModifierKeys modifierKeys = ModifierKeys.None) : this()
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    modifierKeys = modifierKeys | ModifierKeys.Control;
                    key = Key.None;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    modifierKeys = modifierKeys | ModifierKeys.Alt;
                    key = Key.None;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    modifierKeys = modifierKeys | ModifierKeys.Shift;
                    key = Key.None;
                    break;
                case Key.LWin:
                case Key.RWin:
                    modifierKeys = modifierKeys | ModifierKeys.Windows;
                    key = Key.None;
                    break;
            }

            Key = key;
            ModifierKeys = modifierKeys;
        }

        public HotkeyGesture(MouseButton mouseButton, ModifierKeys modifierKeys = ModifierKeys.None) : this()
        {
            MouseButton = mouseButton;
            ModifierKeys = modifierKeys;
        }

        public HotkeyGesture(HotkeyGesture keys, ModifierKeys modifierKeys = ModifierKeys.None) : this()
        {
            MouseButton = keys.MouseButton;
            Key = keys.Key;
            ModifierKeys = modifierKeys;
        }
        
        public HotkeyGesture(MouseButtons mouseButton, ModifierKeys modifierKeys = ModifierKeys.None) : this()
        {
            var button = default(MouseButton?);
            switch (mouseButton)
            {
                case MouseButtons.Left:
                    button = System.Windows.Input.MouseButton.Left;
                    break;
                case MouseButtons.Right:
                    button = System.Windows.Input.MouseButton.Right;
                    break;
                case MouseButtons.Middle:
                    button = System.Windows.Input.MouseButton.Middle;
                    break;
                case MouseButtons.XButton1:
                    button = System.Windows.Input.MouseButton.XButton1;
                    break;
                case MouseButtons.XButton2:
                    button = System.Windows.Input.MouseButton.XButton2;
                    break;
            }

            MouseButton = button;
            ModifierKeys = modifierKeys;
        }

        public MouseButton? MouseButton { get; }

        public Key Key { get; }

        public ModifierKeys ModifierKeys { get; }

        public bool IsEmpty => MouseButton == null && Key == Key.None && ModifierKeys == ModifierKeys.None;

        public bool Equals(HotkeyGesture other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return MouseButton == other.MouseButton && Key == other.Key && ModifierKeys == other.ModifierKeys;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if ((ModifierKeys & ModifierKeys.Control) == ModifierKeys.Control)
            {
                sb.Append(GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_CONTROL));
                sb.Append("+");
            }

            if ((ModifierKeys & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                sb.Append(GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_MENU));
                sb.Append("+");
            }

            if ((ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                sb.Append(GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_SHIFT));
                sb.Append("+");
            }

            if ((ModifierKeys & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                sb.Append("Windows+");
            }

            if (Key != Key.None)
            {
                sb.Append(GetLocalizedKeyString(Key));
            }

            if (MouseButton != null)
            {
                sb.Append($"Mouse{MouseButton}");
            }

            if (sb.Length == 0)
            {
                return "None";
            }

            return sb.ToString().Trim('+');
        }

        private static string GetLocalizedKeyString(Key key)
        {
            if (key >= Key.BrowserBack && key <= Key.LaunchApplication2)
            {
                return key.ToString();
            }

            var virtualKey = (User32.VirtualKey)KeyInterop.VirtualKeyFromKey(key);
            return GetLocalizedKeyStringUnsafe(virtualKey) ?? key.ToString();
        }

        private static string GetLocalizedKeyStringUnsafe(User32.VirtualKey key)
        {
            // strip any modifier keys
            var keyCode = (int)key & 0xffff;

            var sb = new StringBuilder(256);
            var scanCode = User32.MapVirtualKey(keyCode, User32.MapVirtualKeyTranslation.MAPVK_VK_TO_VSC);

            // shift the scan code to the high word
            scanCode = scanCode << 16;
            if (keyCode == 45 ||
                keyCode == 46 ||
                keyCode == 144 ||
                33 <= keyCode && keyCode <= 40)
            {
                // add the extended key flag
                scanCode |= 0x1000000;
            }

            var resultLength = UnsafeNative.GetKeyNameText(scanCode, sb, 256);
            return resultLength > 0 ? sb.ToString() : null;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((HotkeyGesture) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MouseButton?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (int) Key;
                hashCode = (hashCode * 397) ^ (int) ModifierKeys;
                return hashCode;
            }
        }
    }
}