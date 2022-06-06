﻿using System.Runtime.CompilerServices;
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
        var newLogData = log.Data.WithPrefix(() =>
        {
            var prefix = SafeInvoke(log, prefixSupplier);
            return string.IsNullOrEmpty(prefix) ? null : $"[{prefix}] ";
        });
        return log.WithLogData(newLogData);
    }

    public static IFluentLog WithPrefix<T>(this IFluentLog log, T prefix)
    {
        return WithPrefix(log, () => $"{prefix}");
    }

    public static IFluentLog WithSuffix(this IFluentLog log, Func<string> suffixSupplier)
    {
        var newLogData = log.Data.WithSuffix(() =>
        {
            var suffix = SafeInvoke(log, suffixSupplier);
            return string.IsNullOrEmpty(suffix) ? null : $" [{suffix}]";
        });
        return log.WithLogData(newLogData);
    }

    public static IFluentLog WithSuffix<T>(this IFluentLog log, T prefix)
    {
        return WithSuffix(log, () => $"{prefix}");
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

    private static string SafeInvoke(IFluentLog log, Func<string> supplier)
    {
        try
        {
            return supplier();
        }
        catch (Exception e)
        {
            SharedLog.Instance.Log.Warn($"Failed to format log string for logger {log}, data: {new { log.Data.LogLevel, log.Data.Message, log.Data.Exception }}", e);
            return $"FORMATTING ERROR - {e.Message}";
        }
    }

    private static IFluentLog WithConsumer(this IFluentLog log, Action<LogData> messageConsumer)
    {
        var writerAdapter = new LogWriterAdapter<string>(logData =>
        {
            log.Writer.WriteLog(logData);
            messageConsumer(logData);
        });
        return new FluentLogBuilder(writerAdapter);
    }
}