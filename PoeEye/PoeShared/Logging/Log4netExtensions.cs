using log4net;

namespace PoeShared.Logging;

public static class Log4NetExtensions
{
    public static IFluentLog ToFluent(this ILog logger)
    {
        return new FluentLogBuilder(new Log4NetAdapter(logger));
    }
}