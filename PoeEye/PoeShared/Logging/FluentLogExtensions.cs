﻿using System.Runtime.CompilerServices;
using System.Text;
using DynamicData;

namespace PoeShared.Logging;

public static class FluentLogExtensions
{
    public static bool IsEnabled(this IFluentLog log, FluentLogLevel logLevel)
    {
        return logLevel switch
        {
            FluentLogLevel.Trace => log.IsDebugEnabled,
            FluentLogLevel.Debug => log.IsDebugEnabled,
            FluentLogLevel.Info => log.IsInfoEnabled,
            FluentLogLevel.Warn => log.IsWarnEnabled,
            FluentLogLevel.Error => log.IsErrorEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, $"Unknown log level: {logLevel}")
        };
    }

    public static void Write(this IFluentLog log, FluentLogLevel logLevel, Func<string> messageSupplier)
    {
        if (!log.IsEnabled(logLevel))
        {
            return;
        }

        switch (logLevel)
        {
            case FluentLogLevel.Trace:
            case FluentLogLevel.Debug:
                log.Debug(messageSupplier);
                break;
            case FluentLogLevel.Info:
                log.Info(messageSupplier);
                break;
            case FluentLogLevel.Warn:
                log.Warn(messageSupplier);
                break;
            case FluentLogLevel.Error:
                log.Error(messageSupplier);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void DebugIfDebug(this IFluentLog log, Func<string> messageSupplier)
    {
#if DEBUG
        log.Debug(messageSupplier);
#endif
    }

    public static void DebugIfDebug(this IFluentLog log, string message)
    {
#if DEBUG
        log.Debug(message);
#endif
    }

    public static void WarnIfDebug(this IFluentLog log, Func<string> messageSupplier)
    {
#if DEBUG
        log.Warn(messageSupplier);
#endif
    }

    public static void WarnIfDebug(this IFluentLog log, string message)
    {
#if DEBUG
        log.Warn(message);
#endif
    }

    public static void InfoIfDebug(this IFluentLog log, Func<string> messageSupplier)
    {
#if DEBUG
        log.Info(messageSupplier);
#endif
    }
    
    public static void InfoIfDebug(this IFluentLog log, string message)
    {
#if DEBUG
        log.Info(message);
#endif
    }
    
    public static IFluentLog WithLogAction(this IFluentLog log, Action<(FluentLogLevel Level, string Message, string Prefix, string Suffix, Exception Exception)> messageConsumer)
    {
        return WithConsumer(log, logData =>
        {
            messageConsumer((logData.LogLevel, logData.Message, logData.PrefixProvider?.Invoke(), logData.SuffixProvider?.Invoke(), logData.Exception));
        });
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
        return log.WithLogData(log.Data.WithPrefix(prefixSupplier, brackets: true));
    }

    public static IFluentLog WithPrefix<T>(this IFluentLog log, T prefix)
    {
        return log.WithLogData(log.Data.WithPrefix(prefix));
    }
    
    public static IFluentLog WithMaxLineLength(this IFluentLog log, int? value)
    {
        return log.WithLogData(log.Data.WithMaxLineLength(value));
    }
    
    public static IFluentLog WithoutMaxLineLength(this IFluentLog log)
    {
        return log.WithLogData(log.Data.WithMaxLineLength(int.MaxValue));
    }

    public static IFluentLog WithMinLogLevelOverride(this IFluentLog log, FluentLogLevel? value)
    {
        return log.WithLogData(log.Data.WithMinLogLevelOverride(value));
    }
    
    public static IFluentLog WithSuffix(this IFluentLog log, Func<string> suffixSupplier)
    {
        return log.WithLogData(log.Data.WithSuffix(suffixSupplier, brackets: true));
    }

    public static IFluentLog WithSuffix<T>(this IFluentLog log, T suffix)
    {
        return log.WithLogData(log.Data.WithSuffix(suffix));
    }

    public static void AddPrefix(this IFluentLog log, Func<string> prefixSupplier)
    {
        log.Data = log.Data.WithPrefix(prefixSupplier, brackets: true);
    }

    public static void AddPrefix<T>(this IFluentLog log, T prefix)
    {
        log.Data = log.Data.WithPrefix(prefix);
    }

    public static void AddSuffix(this IFluentLog log, Func<string> suffixSupplier)
    {
        log.Data = log.Data.WithSuffix(suffixSupplier, brackets: true);
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
        }, brackets: false);
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