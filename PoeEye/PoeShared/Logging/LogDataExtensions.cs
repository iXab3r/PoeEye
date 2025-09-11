namespace PoeShared.Logging;

internal static class LogDataExtensions
{
    public static LogData WithPrefix<T>(this LogData logData, T prefix)
    {
        return logData.WithPrefix(() => prefix.DumpToString());
    }
    
    public static LogData WithMaxLineLength(this LogData logData, int? value)
    {
        return logData with
        {
            MaxLineLength = value
        };
    }
    
    public static LogData WithMinLogLevelOverride(this LogData logData, FluentLogLevel? value)
    {
        return logData with
        {
            MinLogLevelOverride = value
        };
    }
    
    public static LogData WithPrefix(this LogData logData, Func<string> prefixSupplier, bool brackets = true)
    {
        return logData.WithPrefixProvider(() =>
        {
            var prefix = SafeInvoke(logData, prefixSupplier);
            return string.IsNullOrEmpty(prefix) || !brackets ? prefix : $"[{prefix}] ";
        });
    }

    public static LogData WithSuffix<T>(this LogData logData, T suffix)
    {
        return logData.WithSuffix(() => suffix.DumpToString());
    }
    
    public static LogData WithSuffix(this LogData logData, Func<string> suffixSupplier, bool brackets = true)
    {
        return logData.WithSuffixProvider(() =>
        {
            var suffix = SafeInvoke(logData, suffixSupplier);
            return string.IsNullOrEmpty(suffix) || !brackets ? suffix : $" [{suffix}]";
        });
    }
    
    private static LogData WithSuffixProvider(this LogData logData, Func<string> provider)
    {
        var initial = logData.SuffixProvider;
        return logData with
        {
            SuffixProvider = () => $"{provider()}{initial?.Invoke()}"
        };
    }

    private static LogData WithPrefixProvider(this LogData logData, Func<string> provider)
    {
        var initial = logData.PrefixProvider;
        return logData with
        {
            PrefixProvider = () => $"{initial?.Invoke()}{provider()}"
        };
    }
    
    private static string SafeInvoke(LogData logData, Func<string> supplier)
    {
        try
        {
            return supplier();
        }
        catch (Exception e)
        {
            SharedLog.Instance.Log.Warn($"Failed to format log string, data: {new {logData.LogLevel, logData.Message, logData.Exception}}", e);
            return $"FORMATTING ERROR - {e.Message}";
        }
    }
}