using App.Metrics;

namespace PoeShared.Logging;

internal sealed class FluentLogBuilder : IFluentLog
{
    private readonly LogData logData;
    private readonly ILogWriter logWriter;

    public FluentLogBuilder(ILogWriter logWriter, LogData logData = new LogData())
    {
        this.logWriter = logWriter;
        this.logData = logData;
    }

    ILogWriter IFluentLog.Writer => logWriter;
        
    public IMetrics Metrics => App.Metrics.Metrics.Instance ?? throw new InvalidOperationException("Metrics are not initialized yet");

    public bool IsDebugEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Debug;

    public bool IsInfoEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Info;

    public bool IsWarnEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Warn;

    public bool IsErrorEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Error;

    LogData IFluentLog.Data => logData;

    IFluentLog IFluentLog.WithLogData(LogData newLogData)
    {
        return new FluentLogBuilder(logWriter, newLogData);
    }

    public void Debug(string message)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        Debug(message, default);
    }

    public void Debug(string message, Exception exception)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        var newLogData = logData;
        newLogData.LogLevel = FluentLogLevel.Debug;
        newLogData.Message = message;
        newLogData.Exception = exception;
        logWriter.WriteLog(newLogData);
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

        var newLogData = logData;
        newLogData.LogLevel = FluentLogLevel.Info;
        newLogData.Message = message;
        newLogData.Exception = exception;
        logWriter.WriteLog(newLogData);
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

        var newLogData = logData;
        newLogData.LogLevel = FluentLogLevel.Warn;
        newLogData.Message = message;
        newLogData.Exception = exception;
        logWriter.WriteLog(newLogData);
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

        var newLogData = logData;
        newLogData.LogLevel = FluentLogLevel.Error;
        newLogData.Message = message;
        newLogData.Exception = exception;
        logWriter.WriteLog(newLogData);
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

        var messageString = SafeExtract(message);
        Debug(messageString, exception);
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

        var messageString = SafeExtract(message);
        Info(messageString, exception);
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

        var messageString = SafeExtract(message);
        Warn(messageString, exception);
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

        var messageString = SafeExtract(message);
        Error(messageString, exception);
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

        var messageString = SafeExtract(message);
        Debug(messageString, exception);
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

        var messageString = SafeExtract(message);
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

        var messageString = SafeExtract(message);
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

        var messageString = SafeExtract(message);
        Error(messageString, exception);
    }

    private string SafeExtract(FormattableString supplier)
    {
        try
        {
            return supplier.ToString();
        }
        catch (Exception e)
        {
            var errorMessage = $"Failed to write formatted log message, message: {supplier.Format}, argsCount: {supplier.ArgumentCount}, args: {supplier.GetArguments().DumpToString()}";
            Error(errorMessage, e);
            return default;
        }
    }

    private string SafeExtract(Func<string> supplier)
    {
        try
        {
            return supplier();
        }
        catch (Exception e)
        {
            Warn("Failed to write log message", e);
            return $"Internal logger error: {e.Message}";
        }
    }
}