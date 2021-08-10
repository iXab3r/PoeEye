using System;
using System.Collections.Generic;

namespace PoeShared.Logging
{
    internal sealed class LogWriterAdapter<T> : ILogWriter
    {
        private readonly Action<LogData> logAction;

        public LogWriterAdapter(Action<LogData> logAction)
        {
            this.logAction = logAction;
        }

        public bool IsDebugEnabled => true;

        public bool IsInfoEnabled => true;

        public bool IsWarnEnabled => true;

        public bool IsErrorEnabled => true;

        public void WriteLog(LogData logData)
        {
            logAction(logData);
        }
    }
}