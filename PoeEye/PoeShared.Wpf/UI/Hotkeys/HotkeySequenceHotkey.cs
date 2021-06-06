using System.Drawing;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        public HotkeyGesture Hotkey { get; init; }

        public Point? MousePosition { get; init; }

        public bool IsMouse => Hotkey?.MouseButton != null || HasMousePosition;

        public bool HasMousePosition => MousePosition != null && !MousePosition.Value.IsEmpty;

        public bool? IsDown { get; init; }
    }
}