using System;
using System.Collections.Generic;

namespace PoeShared.Logging
{
    /// <summary>
    ///     A fluent <see langword="interface" /> to build log messages.
    /// </summary>
    public interface IFluentLog
    {
        IFluentLog WithPrefix(object prefix);
        
        IFluentLog WithTable<T>(IEnumerable<T> items, string separator = "\n\t");
        
        void Debug(FormattableString message);

        void Debug(FormattableString message, Exception exception);

        void Info(FormattableString message);

        void Info(FormattableString message, Exception exception);

        void Warn(FormattableString message);

        void Warn(FormattableString message, Exception exception);

        void Error(FormattableString message);

        void Error(FormattableString message, Exception exception);

        bool IsDebugEnabled { get; }

        bool IsInfoEnabled { get; }

        bool IsWarnEnabled { get; }

        bool IsErrorEnabled { get; }
    }
}