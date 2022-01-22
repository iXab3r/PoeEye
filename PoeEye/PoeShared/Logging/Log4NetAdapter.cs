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
        if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
        {
            return Environment.CurrentManagedThreadId.ToString();
        }

        if (Thread.CurrentThread.Name == ".NET ThreadPool Worker")
        {
            return $"Pool#{Environment.CurrentManagedThreadId}";
        }
            
        if (Thread.CurrentThread.Name == ".NET Long Running Task")
        {
            return $"Task#{Environment.CurrentManagedThreadId}";
        }

        return Thread.CurrentThread.Name;
    }

    /// <summary>
    ///     Writes the specified LogData to log4net.
    /// </summary>
    /// <param name="logData">The log data.</param>
    public void WriteLog(LogData logData)
    {
        if (ThreadContext.Properties["threadid"] is not string)
        {
            ThreadContext.Properties["threadid"] = GetThreadName();
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