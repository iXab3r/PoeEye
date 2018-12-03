namespace PoeShared.Native
{
    public struct WinEventHookArguments
    {
        public uint EventMin { get; set; }
        public uint EventMax { get; set; }
        public uint ProcessId { get; set; }
        public uint ThreadId { get; set; }
        public uint Flags { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(EventMin)}: {EventMin}, {nameof(EventMax)}: {EventMax}, {nameof(ProcessId)}: {ProcessId}, {nameof(ThreadId)}: {ThreadId}, {nameof(Flags)}: {Flags}";
        }
    }
}