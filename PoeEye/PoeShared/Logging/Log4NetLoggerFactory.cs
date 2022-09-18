using Microsoft.Extensions.Logging;

namespace PoeShared.Logging;

public sealed class Log4NetLoggerFactory : DisposableReactiveObject, ILoggerFactory
{
    private readonly ILoggerProvider loggerProvider;

    public Log4NetLoggerFactory(ILoggerProvider loggerProvider)
    {
        this.loggerProvider = loggerProvider.AddTo(Anchors);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return loggerProvider.CreateLogger(categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotSupportedException("This factory does not support additional providers");
    }
}