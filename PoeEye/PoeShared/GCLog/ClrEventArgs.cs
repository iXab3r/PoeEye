namespace PoeShared.GCLog;

internal class ClrEventArgs : EventArgs
{
    public ClrEventArgs(DateTime timestamp, int processId)
    {
        TimeStamp = timestamp;
        ProcessId = processId;
    }

    public DateTime TimeStamp { get; }

    public int ProcessId { get; }
}