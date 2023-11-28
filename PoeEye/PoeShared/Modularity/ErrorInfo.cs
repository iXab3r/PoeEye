using Newtonsoft.Json;

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

    public DateTimeOffset Timestamp { get; init; }
    
    public string Message { get; init; }

    public Exception Error { get; init; }

    public ErrorInfoId Id { get; } = new(Guid.NewGuid());
    
    [JsonIgnore]
    public bool IsEmpty => Empty.Equals(this);

    public static implicit operator ErrorInfo(Exception e) => FromException(e);

    public static ErrorInfo FromMessage(string message)
    {
        return new ErrorInfo()
        {
            Message = message,
            Timestamp = DateTimeOffset.Now
        };
    }
    
    public static ErrorInfo FromException(Exception error)
    {
        return new ErrorInfo()
        {
            Error = error,
            Message = error.Message,
            Timestamp = DateTimeOffset.Now
        };
    }
}