using System;
using PInvoke;

namespace PoeShared.Native;

public readonly struct WinEventHookData
{
    public WinEventHookData(
        User32.WindowsEventHookType eventId,
        IntPtr windowHandle,
        int childId,
        int objectId,
        int eventThreadId,
        uint eventTimeInMs,
        IntPtr winEventHookHandle)
    {
        EventId = eventId;
        WindowHandle = windowHandle;
        ChildId = childId;
        ObjectId = objectId;
        EventThreadId = eventThreadId;
        EventTimeInMs = eventTimeInMs;
        WinEventHookHandle = winEventHookHandle;
    }
    
    public IntPtr WinEventHookHandle { get; init; }
    public User32.WindowsEventHookType EventId { get; init; }
    public IntPtr WindowHandle { get; init; }
    public int ObjectId { get; init; }
    public int ChildId { get; init; }
    public int EventThreadId { get; init; }
    public uint EventTimeInMs { get; init; }
}