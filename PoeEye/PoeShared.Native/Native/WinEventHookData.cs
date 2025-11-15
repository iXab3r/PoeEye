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

    public readonly IntPtr WinEventHookHandle;
    public readonly User32.WindowsEventHookType EventId;
    public readonly IntPtr WindowHandle;
    public readonly int ObjectId;
    public readonly int ChildId;
    public readonly int EventThreadId;
    public readonly uint EventTimeInMs;
}