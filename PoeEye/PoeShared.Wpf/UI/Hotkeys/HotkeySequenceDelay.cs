using System;

namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceDelay : HotkeySequenceItem
    {
        public TimeSpan Delay { get; init; }

        public bool IsKeypress { get; init; }
    }
}