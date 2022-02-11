namespace PoeShared.GCLog;

internal sealed class ClrFinalizeArgs : ClrEventArgs
{
    public ClrFinalizeArgs(DateTime timeStamp, int processId, ulong typeId, string typeName)
        : base(timeStamp, processId)
    {
        TypeId = typeId;
        TypeName = typeName;
    }

    public ulong TypeId { get; }

    public string TypeName { get; }
}