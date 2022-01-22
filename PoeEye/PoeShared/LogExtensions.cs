using JetBrains.Annotations;
using log4net;
using LogLevel = Splat.LogLevel;

namespace PoeShared;

public static class LogExtensions
{
    public static void HandleException(this ILog logger, [NotNull] Exception exception)
    {
        Guard.ArgumentNotNull(logger, nameof(logger));
        Guard.ArgumentNotNull(exception, nameof(exception));

        logger.Error("Exception occurred", exception);
    }

    public static void HandleUiException(this ILog logger, [NotNull] Exception exception)
    {
        Guard.ArgumentNotNull(exception, nameof(exception));

        logger.Error("UI Exception occurred", exception);
        SharedLog.Instance.Errors.OnNext(exception);
    }
        
    public static void HandleException(this IFluentLog logger, [NotNull] Exception exception)
    {
        Guard.ArgumentNotNull(logger, nameof(logger));
        Guard.ArgumentNotNull(exception, nameof(exception));

        logger.Error($"Exception occurred", exception);
    }

    public static void HandleUiException(this IFluentLog logger, [NotNull] Exception exception)
    {
        Guard.ArgumentNotNull(exception, nameof(exception));
        logger.Error($"UI Exception occurred", exception);
        SharedLog.Instance.Errors.OnNext(exception);
    }
        
    public static void LogIfThrows(this IFluentLog This, LogLevel level, string message, Action block)
    {
        try
        {
            block();
        }
        catch (Exception ex)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    This.Debug(message ?? "", ex);
                    break;
                case LogLevel.Info:
                    This.Info(message ?? "", ex);
                    break;
                case LogLevel.Warn:
                    This.Warn(message ?? "", ex);
                    break;
                case LogLevel.Error:
                    This.Error(message ?? "", ex);
                    break;
            }

            throw;
        }
    }

    public static async Task LogIfThrows(this IFluentLog This, LogLevel level, string message, Func<Task> block)
    {
        try
        {
            await block();
        }
        catch (Exception ex)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    This.Debug(message ?? "", ex);
                    break;
                case LogLevel.Info:
                    This.Info(message ?? "", ex);
                    break;
                case LogLevel.Warn:
                    This.Warn(message ?? "", ex);
                    break;
                case LogLevel.Error:
                    This.Error(message ?? "", ex);
                    break;
            }

            throw;
        }
    }

    public static async Task<T> LogIfThrows<T>(this IFluentLog This, LogLevel level, string message, Func<Task<T>> block)
    {
        try
        {
            return await block();
        }
        catch (Exception ex)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    This.Debug(message ?? "", ex);
                    break;
                case LogLevel.Info:
                    This.Info(message ?? "", ex);
                    break;
                case LogLevel.Warn:
                    This.Warn(message ?? "", ex);
                    break;
                case LogLevel.Error:
                    This.Error(message ?? "", ex);
                    break;
            }

            throw;
        }
    }

    public static void WarnIfThrows(this IFluentLog This, Action block, string message = null)
    {
        This.LogIfThrows(LogLevel.Warn, message, block);
    }

    public static Task WarnIfThrows(this IFluentLog This, Func<Task> block, string message = null)
    {
        return This.LogIfThrows(LogLevel.Warn, message, block);
    }

    public static Task<T> WarnIfThrows<T>(this IFluentLog This, Func<Task<T>> block, string message = null)
    {
        return This.LogIfThrows(LogLevel.Warn, message, block);
    }

    public static void ErrorIfThrows(this IFluentLog This, Action block, string message = null)
    {
        This.LogIfThrows(LogLevel.Error, message, block);
    }

    public static Task ErrorIfThrows(this IFluentLog This, Func<Task> block, string message = null)
    {
        return This.LogIfThrows(LogLevel.Error, message, block);
    }

    public static Task<T> ErrorIfThrows<T>(this IFluentLog This, Func<Task<T>> block, string message = null)
    {
        return This.LogIfThrows(LogLevel.Error, message, block);
    }
}