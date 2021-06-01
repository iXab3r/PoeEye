using System;
using System.Drawing;
using System.Windows.Input;

namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        public HotkeyGesture Hotkey { get; init; }

        public Point? MousePosition { get; init; }

        public bool IsMouse => (Hotkey?.MouseButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle) || HasMousePosition;

        public bool HasMousePosition => MousePosition != null && !MousePosition.Value.IsEmpty;

        public bool? IsDown { get; init; }
    }
}