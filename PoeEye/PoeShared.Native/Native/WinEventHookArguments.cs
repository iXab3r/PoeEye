using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public readonly struct WinEventHookArguments
{
    public User32.WindowsEventHookType EventMin { get; init; }
    public User32.WindowsEventHookType EventMax { get; init; }
    public int ProcessId { get; init; }
    public int ThreadId { get; init; }
    public User32.WindowsEventHookFlags Flags { get; init; }

    public override string ToString()
    {
        var builder = new ToStringBuilder("HookArgs");
        builder.AppendParameterIfNotDefault(nameof(EventMin), EventMin);
        builder.AppendParameterIfNotDefault(nameof(EventMax), EventMax);
        builder.AppendParameterIfNotDefault(nameof(ProcessId), ProcessId);
        builder.AppendParameterIfNotDefault(nameof(ThreadId), ThreadId);
        builder.AppendParameterIfNotDefault(nameof(Flags), Flags);
        return builder.ToString();
    }
}