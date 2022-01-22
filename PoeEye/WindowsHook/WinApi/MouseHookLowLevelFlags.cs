using System;

namespace WindowsHook.WinApi;

[Flags]
internal enum MouseHookLowLevelFlags : int
{
    None = 0x00000000,
    /// <summary>
    /// Test the event-injected (from any process) flag.
    /// </summary>
    LLMHF_INJECTED = 0x00000001,
    /// <summary>
    /// Test the event-injected (from a process running at lower integrity level) flag.
    /// </summary>
    LLMHF_LOWER_IL_INJECTED = 0x00000002,
}