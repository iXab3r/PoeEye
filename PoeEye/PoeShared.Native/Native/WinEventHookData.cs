using System;
using PInvoke;

namespace PoeShared.Native;

public sealed record WinEventHookData
{
    public IntPtr WinEventHookHandle { get; init; }
    public User32.WindowsEventHookType EventId { get; init; }
    public IntPtr WindowHandle { get; init; }
    public int ObjectId { get; init; }
    public int ChildId { get; init; }
    public int EventThreadId { get; init; }
    public uint EventTimeInMs { get; init; }
}