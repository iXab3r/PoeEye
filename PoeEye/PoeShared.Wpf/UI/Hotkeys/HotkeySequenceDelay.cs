using System;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceDelay : HotkeySequenceItem
    {
        public TimeSpan Delay { get; init; }

        public bool IsKeypress { get; init; }
    }
}