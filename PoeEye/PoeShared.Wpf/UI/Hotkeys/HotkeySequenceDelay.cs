using System;

namespace PoeShared.UI;

public sealed class HotkeySequenceDelay : HotkeySequenceItem
{
    public TimeSpan Delay { get; set; }

    public bool IsKeypress { get; set; }
}