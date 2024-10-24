using System.Threading;
using log4net;

namespace PoeShared.Logging;

internal sealed class Log4NetAdapter : ILogWriter
{
    private readonly ILog logger;

    public Log4NetAdapter(ILog logger)
    {
        this.logger = logger;
    }

    public bool IsDebugEnabled => logger.IsDebugEnabled;

    public bool IsInfoEnabled => logger.IsInfoEnabled;

    public bool IsWarnEnabled => logger.IsWarnEnabled;

    public bool IsErrorEnabled => logger.IsErrorEnabled;

    private static string GetThreadName()
    {
        string threadName = Thread.CurrentThread.Name switch
        {
            ".NET ThreadPool Worker" => $"Pool",
            ".NET Long Running Task" => $"Task",
            _ => Thread.CurrentThread.Name
        };

        return $"{threadName}@{Environment.CurrentManagedThreadId}";
    }

    /// <summary>
    ///     Writes the specified LogData to log4net.
    /// </summary>
    /// <param name="logData">The log data.</param>
    public void WriteLog(LogData logData)
    {
        if (ThreadContext.Properties["threadid"] is not string)
        {
            ThreadContext.Properties["threadid"] = $"{GetThreadName()}";
        }
           
        switch (logData.LogLevel)
        {
            case FluentLogLevel.Info:
                if (logger.IsInfoEnabled)
                {
                    WriteLog(logData, logger.Info, logger.Info);
                }
                break;
            case FluentLogLevel.Warn:
                if (logger.IsWarnEnabled)
                {
                    WriteLog(logData, logger.Warn, logger.Warn);
                }
                break;
            case FluentLogLevel.Error:
                if (logger.IsErrorEnabled)
                {
                    WriteLog(logData, logger.Error, logger.Error);
                }
                break;
            default:
                if (logger.IsDebugEnabled)
                {
                    WriteLog(logData, logger.Debug, logger.Debug);
                }
                break;
        }
    }

    private static void WriteLog(LogData logData, Action<object> logAction, Action<object, Exception> errorAction)
    {
        var message = logData.ToString();
        if (logData.Exception == null)
        {
            logAction(message);
        }
        else
        {
            errorAction(message, logData.Exception);
        }
    }
}