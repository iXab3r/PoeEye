using System;

namespace WindowsHook.WinApi;

[Flags]
internal enum KeyboardHookLowLevelFlags : int
{
    None = 0x00000000,
    /// <summary>
    /// Test the event-injected (from a process running at lower integrity level) flag.
    /// </summary>
    LLKHF_LOWER_IL_INJECTED = 0x00000002,
    /// <summary>
    /// Test the event-injected (from any process) flag.
    /// </summary>
    LLKHF_INJECTED = 0x00000010,
}