using log4net;
using Microsoft.Extensions.Logging;

namespace PoeShared.Logging;

public sealed class Logger4Log4Net : ILogger
{
    private readonly ILog logger;

    public Logger4Log4Net(string name)
    {
        var repository = LogManager.GetAllRepositories().FirstOrDefault();
        logger = LogManager.GetLogger(repository?.Name, name);
    }

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, 
        TState state, Exception exception, 
        Func<TState, Exception, string> formatter)
    {
        var message = formatter != null
            ? formatter(state, exception)
            : "null";

        switch (logLevel)
        {
            case LogLevel.None:
            case LogLevel.Trace:
            case LogLevel.Debug:
                logger.Debug(message, exception);
                break;
            case LogLevel.Information:
                logger.Info(message, exception);
                break;
            case LogLevel.Warning:
                logger.Warn(message, exception);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                logger.Error(message, exception);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }
 
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => logger.IsDebugEnabled,
            LogLevel.Debug => logger.IsDebugEnabled,
            LogLevel.Information => logger.IsInfoEnabled,
            LogLevel.Warning => logger.IsWarnEnabled,
            LogLevel.Error => logger.IsErrorEnabled,
            LogLevel.Critical => logger.IsFatalEnabled,
            var _ => false
        };
    }
 
    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }
}