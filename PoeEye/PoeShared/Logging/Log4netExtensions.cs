using log4net;
using log4net.Core;

namespace PoeShared.Logging;

public static class Log4NetExtensions
{
    public static IFluentLog ToFluent(this ILog logger)
    {
        return new FluentLogBuilder(new Log4NetAdapter(logger));
    }

    public static Level ToLog4NetLevel(this FluentLogLevel level)
    {
        return level switch
        {
            FluentLogLevel.Trace => Level.Trace,
            FluentLogLevel.Debug => Level.Debug,
            FluentLogLevel.Info => Level.Info,
            FluentLogLevel.Warn => Level.Warn,
            FluentLogLevel.Error => Level.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, $"Unknown log level: {level}")
        };
    }
}