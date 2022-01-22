using PInvoke;

namespace PoeShared.Native;

public struct WinEventHookArguments
{
    public User32.WindowsEventHookType EventMin { get; init; }
    public User32.WindowsEventHookType EventMax { get; init; }
    public int ProcessId { get; init; }
    public int ThreadId { get; init; }
    public User32.WindowsEventHookFlags Flags { get; init; }

    public override string ToString()
    {
        return
            $"{(EventMin != EventMax ? $"{nameof(EventMin)}: {EventMin}, {nameof(EventMax)}: {EventMax}" : $"Event: {EventMin}")}, {nameof(ProcessId)}: {ProcessId}, {nameof(ThreadId)}: {ThreadId}, {nameof(Flags)}: {Flags}";
    }
}