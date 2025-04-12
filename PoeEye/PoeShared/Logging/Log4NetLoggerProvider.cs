using Microsoft.Extensions.Logging;

namespace PoeShared.Logging;

public sealed class Log4NetLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ILogger> loggers;
 
    public Log4NetLoggerProvider()
    {
        loggers = new ConcurrentDictionary<string, ILogger>();
    }
 
    public void Dispose()
    {
    }
    
    public LogLevel MinimumLevel { get; set; }
 
    public ILogger CreateLogger(string categoryName)
    {
        return loggers.GetOrAdd(categoryName, x => new Logger4Log4Net(categoryName)
        {
            MinimumLevel = MinimumLevel
        });
    }
}