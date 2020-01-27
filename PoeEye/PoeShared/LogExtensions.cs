using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using Splat;

namespace PoeShared
{
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
        
        public static void LogIfThrows(this ILog This, LogLevel level, string message, Action block)
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

        public static async Task LogIfThrows(this ILog This, LogLevel level, string message, Func<Task> block)
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

        public static async Task<T> LogIfThrows<T>(this ILog This, LogLevel level, string message, Func<Task<T>> block)
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

        public static void WarnIfThrows(this ILog This, Action block, string message = null)
        {
            This.LogIfThrows(LogLevel.Warn, message, block);
        }

        public static Task WarnIfThrows(this ILog This, Func<Task> block, string message = null)
        {
            return This.LogIfThrows(LogLevel.Warn, message, block);
        }

        public static Task<T> WarnIfThrows<T>(this ILog This, Func<Task<T>> block, string message = null)
        {
            return This.LogIfThrows(LogLevel.Warn, message, block);
        }

        public static void ErrorIfThrows(this ILog This, Action block, string message = null)
        {
            This.LogIfThrows(LogLevel.Error, message, block);
        }

        public static Task ErrorIfThrows(this ILog This, Func<Task> block, string message = null)
        {
            return This.LogIfThrows(LogLevel.Error, message, block);
        }

        public static Task<T> ErrorIfThrows<T>(this ILog This, Func<Task<T>> block, string message = null)
        {
            return This.LogIfThrows(LogLevel.Error, message, block);
        }
    }
}