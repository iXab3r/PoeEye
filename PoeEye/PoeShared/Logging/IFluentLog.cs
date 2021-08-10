using System;

namespace PoeShared.Logging
{
    /// <summary>
    ///     A fluent <see langword="interface" /> to build log messages.
    /// </summary>
    public interface IFluentLog
    {
        internal LogData Data { get; }
        
        internal ILogWriter Writer { get; }

        bool IsDebugEnabled { get; }

        bool IsInfoEnabled { get; }

        bool IsWarnEnabled { get; }

        bool IsErrorEnabled { get; }
        
        internal IFluentLog WithLogData(LogData newLogData);

        void Debug(string message);

        void Debug(string message, Exception exception);

        void Info(string message);

        void Info(string message, Exception exception);

        void Warn(string message);

        void Warn(string message, Exception exception);

        void Error(string message);

        void Error(string message, Exception exception);

        void Debug(Func<string> message);

        void Debug(Func<string> message, Exception exception);

        void Info(Func<string> message);

        void Info(Func<string> message, Exception exception);

        void Warn(Func<string> message);

        void Warn(Func<string> message, Exception exception);

        void Error(Func<string> message);

        void Error(Func<string> message, Exception exception);

        void Debug(FormattableString message);

        void Debug(FormattableString message, Exception exception);

        void Info(FormattableString message);

        void Info(FormattableString message, Exception exception);

        void Warn(FormattableString message);

        void Warn(FormattableString message, Exception exception);

        void Error(FormattableString message);

        void Error(FormattableString message, Exception exception);
    }
}