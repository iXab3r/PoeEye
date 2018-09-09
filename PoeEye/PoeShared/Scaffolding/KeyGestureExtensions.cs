using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace PoeShared.Scaffolding
{
    public static class KeyGestureExtensions
    {
        public static bool MatchesHotkey(this KeyGesture candidate, WinFormsKeyEventArgs args)
        {
            if (args == null || candidate == null)
            {
                return false;
            }
            var winKey = (Keys)KeyInterop.VirtualKeyFromKey(candidate.Key);
            var keyMatches = args.KeyCode == winKey;
            var wpfModifiers = ModifierKeys.None;
            if (args.Alt && winKey != Keys.Alt)
            {
                wpfModifiers |= ModifierKeys.Alt;
            }
            if (args.Control && winKey != Keys.LControlKey && winKey != Keys.RControlKey)
            {
                wpfModifiers |= ModifierKeys.Control;
            }
            if (args.Shift && winKey != Keys.Shift && winKey != Keys.ShiftKey && winKey != Keys.RShiftKey &&
                winKey != Keys.LShiftKey)
            {
                wpfModifiers |= ModifierKeys.Shift;
            }
            return keyMatches && wpfModifiers == candidate.Modifiers;
        }

        public static string[] GetHotkeyList()
        {
            var keyGestureConverter = new KeyGestureConverter();
            return Enum.GetValues(typeof(Key))
                .OfType<Key>()
                .Select(TryToCreateKeyGesture)
                .Select(x => x.Key == Key.None ? "None" : keyGestureConverter.ConvertToInvariantString(x))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }

        public static KeyGesture SafeCreateGesture(string hotkeyString)
        {
            try
            {
                return new KeyGestureConverter().ConvertFromInvariantString(hotkeyString) as KeyGesture;
            }
            catch (Exception)
            {
                return new KeyGesture(Key.None);
            }
        }

        private static KeyGesture TryToCreateKeyGesture(Key key)
        {
            try
            {
                return new KeyGesture(key);
            }
            catch (Exception)
            {
                return new KeyGesture(Key.None);
            }
        }
    }
}