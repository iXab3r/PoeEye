using System.Runtime.CompilerServices;
using System.Text;
using DynamicData;
using log4net.Core;

namespace PoeShared.Logging;

public static class FluentLogExtensions
{
    public static void DebugIfDebug(this IFluentLog log, Func<string> message)
    {
#if DEBUG
        log.Debug(message);
#endif
    }

    public static void WarnIfDebug(this IFluentLog log, Func<string> message)
    {
#if DEBUG
        log.Warn(message);
#endif
    }

    public static void InfoIfDebug(this IFluentLog log, Func<string> message)
    {
#if DEBUG
        log.Info(message);
#endif
    }

    public static IFluentLog WithAction(this IFluentLog log, Action<(FluentLogLevel Level, string Message)> messageConsumer)
    {
        return WithConsumer(log, logData =>
        {
            var message = logData.ToString();
            if (logData.Exception != null)
            {
                message += Environment.NewLine + logData.Exception.Message + Environment.NewLine + logData.Exception.StackTrace;
            }

            messageConsumer((logData.LogLevel, message));
        });
    }

    public static IFluentLog CreateChildCollectionLogWriter(this IFluentLog log, ISourceList<string> collection)
    {
        return log.WithAction(x => collection.Add(x.Message));
    }

    public static BenchmarkTimer CreateProfiler(this IFluentLog log, string benchmarkName, [CallerMemberName] string propertyName = null)
    {
        return new BenchmarkTimer(benchmarkName, log, propertyName);
    }
    
    public static IFluentLog WithPrefix(this IFluentLog log, Func<string> prefixSupplier)
    {
        return log.WithLogData(log.Data.WithPrefix(prefixSupplier));
    }

    public static IFluentLog WithPrefix<T>(this IFluentLog log, T prefix)
    {
        return log.WithLogData(log.Data.WithPrefix(prefix));
    }

    public static IFluentLog WithSuffix(this IFluentLog log, Func<string> suffixSupplier)
    {
        return log.WithLogData(log.Data.WithSuffix(suffixSupplier));
    }

    public static IFluentLog WithSuffix<T>(this IFluentLog log, T suffix)
    {
        return log.WithLogData(log.Data.WithSuffix(suffix));
    }
    
    public static void AddPrefix(this IFluentLog log, Func<string> prefixSupplier)
    {
        log.Data = log.Data.WithPrefix(prefixSupplier);
    }

    public static void AddPrefix<T>(this IFluentLog log, T prefix)
    {
        log.Data = log.Data.WithPrefix(prefix);
    }

    public static void AddSuffix(this IFluentLog log, Func<string> suffixSupplier)
    {
        log.Data = log.Data.WithSuffix(suffixSupplier);
    }

    public static void AddSuffix<T>(this IFluentLog log, T suffix)
    {
        log.Data = log.Data.WithSuffix(suffix);
    }

    public static IFluentLog WithTable<T>(this IFluentLog log, IEnumerable<T> items, string separator = "\n\t")
    {
        var newLogData = log.Data.WithSuffix(() =>
        {
            var result = new StringBuilder();
            var count = 0;
            foreach (var item in items)
            {
                result.Append(separator);
                count++;
                result.Append($"#{count} {item}");
            }

            return $"{separator}Items: {count}{result}";
        });
        return log.WithLogData(newLogData);
    }

    private static IFluentLog WithConsumer(this IFluentLog log, Action<LogData> messageConsumer)
    {
        var writerAdapter = new LogWriterAdapter<string>(logData =>
        {
            log.Writer.WriteLog(logData);
            messageConsumer(logData);
        });
        return new FluentLogBuilder(writerAdapter, log.Data);
    }
    
    private static IFluentLog WithLogData(this IFluentLog log, LogData newLogData)
    {
        return new FluentLogBuilder(log.Writer, newLogData);
    }
}