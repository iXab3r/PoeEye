namespace PoeShared.Logging;

/// <summary>
///     A fluent <see langword="interface" /> to build log messages.
/// </summary>
public interface IFluentLog
{
    internal LogData Data { get; set; }
        
    internal ILogWriter Writer { get; }
        
    bool IsDebugEnabled { get; }

    bool IsInfoEnabled { get; }

    bool IsWarnEnabled { get; }

    bool IsErrorEnabled { get; }

    void Debug(string message);
    void Debug(Func<string> messageSupplier);
    void Debug(string message, Exception exception);
    
    void Info(string message);
    void Info(Func<string> messageSupplier);
    void Info(string message, Exception exception);
    
    void Warn(string message);
    void Warn(Func<string> messageSupplier);
    void Warn(string message, Exception exception);
    
    void Error(string message);
    void Error(Func<string> messageSupplier);
    void Error(string message, Exception exception);
}