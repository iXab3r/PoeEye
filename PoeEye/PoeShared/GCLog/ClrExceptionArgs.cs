namespace PoeShared.GCLog;

internal sealed class ClrExceptionArgs : ClrEventArgs
{
    public ClrExceptionArgs(DateTime timeStamp, int processId, string typeName, string message)
        : base(timeStamp, processId)
    {
        TypeName = typeName;
        Message = message;
    }

    public string TypeName { get; }

    public string Message { get; }
}