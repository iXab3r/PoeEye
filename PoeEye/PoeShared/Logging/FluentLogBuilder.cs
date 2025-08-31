namespace PoeShared.Logging;

internal sealed class FluentLogBuilder : IFluentLog
{
    private readonly ILogWriter logWriter;

    public FluentLogBuilder(ILogWriter logWriter, LogData data = default)
    {
        Data = data;
        this.logWriter = logWriter;
    }

    ILogWriter IFluentLog.Writer => logWriter;
        
    /// <summary>
    /// Checks whether a given <paramref name="level"/> is enabled.
    /// Writer must allow the level, and the effective threshold must be ≤ level.
    /// </summary>
    public bool IsEnabled(FluentLogLevel level)
        => WriterAllows(logWriter, level) && EffectiveMin <= level;

    /// <summary>True if DEBUG is enabled under the current writer and threshold.</summary>
    public bool IsDebugEnabled => IsEnabled(FluentLogLevel.Debug);

    /// <summary>True if INFO is enabled under the current writer and threshold.</summary>
    public bool IsInfoEnabled  => IsEnabled(FluentLogLevel.Info);

    /// <summary>True if WARN is enabled under the current writer and threshold.</summary>
    public bool IsWarnEnabled  => IsEnabled(FluentLogLevel.Warn);

    /// <summary>True if ERROR is enabled under the current writer and threshold.</summary>
    public bool IsErrorEnabled => IsEnabled(FluentLogLevel.Error);
    
    public LogData Data { get; set; }
    
    private FluentLogLevel EffectiveMin =>
        Data.MinLogLevelOverride ?? FluentLogSettings.Instance.MinLogLevel ?? default;
        
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
        logWriter.WriteLog(newLogData);
    }
    
    public void Debug(string message)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        Debug(message, default);
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
        logWriter.WriteLog(newLogData);
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
        logWriter.WriteLog(newLogData);
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
        logWriter.WriteLog(newLogData);
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
    
    /// <summary>
    /// Maps a level to the writer's own capability flags.
    /// Writer is the boss: if this returns false, logging is off regardless of thresholds.
    /// </summary>
    private static bool WriterAllows(ILogWriter logWriter, FluentLogLevel level) => level switch
    {
        FluentLogLevel.Debug => logWriter.IsDebugEnabled,
        FluentLogLevel.Info  => logWriter.IsInfoEnabled,
        FluentLogLevel.Warn  => logWriter.IsWarnEnabled,
        FluentLogLevel.Error => logWriter.IsErrorEnabled,
        _ => false
    };
}