using App.Metrics;

namespace PoeShared.Logging;

/// <summary>
///     A fluent <see langword="interface" /> to build log messages.
/// </summary>
public interface IFluentLog
{
    internal LogData Data { get; set; }
        
    internal ILogWriter Writer { get; }
        
    IMetrics Metrics { get; }

    bool IsDebugEnabled { get; }

    bool IsInfoEnabled { get; }

    bool IsWarnEnabled { get; }

    bool IsErrorEnabled { get; }
        
    public void Debug(string message, Exception exception)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        var newLogData = Data with
        {
            LogLevel = FluentLogLevel.Debug,
            Message = message,
            Exception = exception
        };
        Writer.WriteLog(newLogData);
    }
    
    public void Debug(string message)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        Debug(message, default);
    }

    public void Debug(FormattableString message)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        Debug(message, default);
    }

    public void Debug(FormattableString message, Exception exception)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Debug(messageString, exception);
    }

    public void Debug(Func<string> message)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        Debug(message, default);
    }

    public void Debug(Func<string> message, Exception exception)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Debug(messageString, exception);
    }
    
    public void Info(string message)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        Info(message, default);
    }

    public void Info(string message, Exception exception)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        var newLogData = Data with
        {
            LogLevel = FluentLogLevel.Info,
            Message = message,
            Exception = exception
        };
        Writer.WriteLog(newLogData);
    }
    
    public void Info(Func<string> message)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        Info(message, default);
    }

    public void Info(Func<string> message, Exception exception)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Info(messageString, exception);
    }

    public void Warn(string message)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        Warn(message, default);
    }

    public void Warn(string message, Exception exception)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        var newLogData = Data with
        {
            LogLevel = FluentLogLevel.Warn,
            Message = message,
            Exception = exception
        };
        Writer.WriteLog(newLogData);
    }
    
    public void Warn(Func<string> message)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        Warn(message, default);
    }

    public void Warn(Func<string> message, Exception exception)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Warn(messageString, exception);
    }

    public void Error(string message)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        Error(message, default);
    }

    public void Error(string message, Exception exception)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        var newLogData = Data with
        {
            LogLevel = FluentLogLevel.Error,
            Message = message,
            Exception = exception
        };
        Writer.WriteLog(newLogData);
    }

    public void Error(Func<string> message)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        Error(message, default);
    }

    public void Error(Func<string> message, Exception exception)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Error(messageString, exception);
    }

    public void Info(FormattableString message)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        Info(message, default);
    }

    public void Info(FormattableString message, Exception exception)
    {
        if (!IsInfoEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Info(messageString, exception);
    }

    public void Warn(FormattableString message)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        Warn(message, default);
    }

    public void Warn(FormattableString message, Exception exception)
    {
        if (!IsWarnEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Warn(messageString, exception);
    }

    public void Error(FormattableString message)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        Error(message, default);
    }

    public void Error(FormattableString message, Exception exception)
    {
        if (!IsErrorEnabled)
        {
            return;
        }

        var messageString = SafeExtract(this, message);
        Error(messageString, exception);
    }

    private static string SafeExtract(IFluentLog log, FormattableString supplier)
    {
        try
        {
            return supplier.ToString();
        }
        catch (Exception e)
        {
            var errorMessage = $"Failed to write formatted log message, message: {supplier.Format}, argsCount: {supplier.ArgumentCount}, args: {supplier.GetArguments().DumpToString()}";
            log.Error(errorMessage, e);
            return default;
        }
    }

    private static string SafeExtract(IFluentLog log, Func<string> supplier)
    {
        try
        {
            return supplier();
        }
        catch (Exception e)
        {
            log.Warn("Failed to write log message", e);
            return $"Internal logger error: {e.Message}";
        }
    }
}