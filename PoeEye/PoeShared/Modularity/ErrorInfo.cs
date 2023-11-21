namespace PoeShared.Modularity;

public readonly record struct ErrorInfo
{
    public static ErrorInfo Empty = new();

    public ErrorInfo()
    {
        Message = null;
        Timestamp = default;
        Error = null;
    }

    public ErrorInfoId Id { get; } = new(Guid.NewGuid());
    
    public string Message { get; init; }
    
    public DateTimeOffset Timestamp { get; init; }
    
    public Exception Error { get; init; }
    
    public bool IsEmpty => Empty.Equals(this);
}