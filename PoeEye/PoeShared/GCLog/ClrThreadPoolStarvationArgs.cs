namespace PoeShared.GCLog;

internal sealed class ClrThreadPoolStarvationArgs : ClrEventArgs
{
    public ClrThreadPoolStarvationArgs(DateTime timestamp, int processId, int workerThreadCount)
        : base(timestamp, processId)
    {
        WorkerThreadCount = workerThreadCount;
    }

    public int WorkerThreadCount { get; set; }
}