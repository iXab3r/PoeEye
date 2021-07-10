using System;

namespace PoeShared.Logging
{
    /// <summary>
    /// An interface defining a log writer.
    /// </summary>
    internal interface ILogWriter
    {
        bool IsDebugEnabled { get; }

        bool IsInfoEnabled { get; }

        bool IsWarnEnabled { get; }

        bool IsErrorEnabled { get; }
        
        /// <summary>
        /// Writes the specified <see cref="LogData"/> to the underlying logger.
        /// </summary>
        /// <param name="logData">The log data to write.</param>
        void WriteLog(LogData logData);
    }
}