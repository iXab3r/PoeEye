using PInvoke;

namespace PoeShared.Native
{
    public struct WinEventHookArguments
    {
        public User32.WindowsEventHookType EventMin { get; set; }
        public User32.WindowsEventHookType EventMax { get; set; }
        public int ProcessId { get; set; }
        public int ThreadId { get; set; }
        public User32.WindowsEventHookFlags Flags { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(EventMin)}: {EventMin}, {nameof(EventMax)}: {EventMax}, {nameof(ProcessId)}: {ProcessId}, {nameof(ThreadId)}: {ThreadId}, {nameof(Flags)}: {Flags}";
        }
    }
}