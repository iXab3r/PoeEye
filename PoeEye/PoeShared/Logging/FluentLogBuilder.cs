using System;
using System.Collections.Generic;
using System.Text;

namespace PoeShared.Logging
{
    internal sealed class FluentLogBuilder : IFluentLog
    {
        private readonly ILogWriter logWriter;
        private readonly LogData logData;

        public FluentLogBuilder(ILogWriter logWriter, LogData logData = new LogData())
        {
            this.logWriter = logWriter;
            this.logData = logData;
        }

        public IFluentLog WithPrefix(object prefix)
        {
            var newLogData = logData.WithPrefix(() => $"[{prefix}] ");
            return new FluentLogBuilder(logWriter, newLogData);
        }

        public IFluentLog WithTable<T>(IEnumerable<T> items, string separator = "\n\t")
        {
            var newLogData = logData.WithSuffix(() =>
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
            return new FluentLogBuilder(logWriter, newLogData);
        }

        public void Debug(FormattableString message)
        {
            if (!IsDebugEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.Message = message.ToString();
            logWriter.WriteLog(newLogData);
        }

        public void Debug(FormattableString message, Exception exception)
        {
            if (!IsDebugEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Debug;
            newLogData.Message = message.ToString();
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Info(FormattableString message)
        {
            if (!IsInfoEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Info;
            newLogData.Message = message.ToString();
            logWriter.WriteLog(newLogData);
        }

        public void Info(FormattableString message, Exception exception)
        {
            if (!IsInfoEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Info;
            newLogData.Message = message.ToString();
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Warn(FormattableString message)
        {
            if (!IsWarnEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Warn;
            newLogData.Message = message.ToString();
            logWriter.WriteLog(newLogData);
        }

        public void Warn(FormattableString message, Exception exception)
        {
            if (!IsWarnEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Warn;
            newLogData.Message = message.ToString();
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Error(FormattableString message)
        {
            if (!IsErrorEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Warn;
            newLogData.Message = message.ToString();
            logWriter.WriteLog(newLogData);
        }

        public void Error(FormattableString message, Exception exception)
        {
            if (!IsErrorEnabled)
            {
                return;
            }
            var newLogData = logData;
            newLogData.LogLevel = LogLevel.Warn;
            newLogData.Message = message.ToString();
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public bool IsDebugEnabled => logWriter.IsDebugEnabled;

        public bool IsInfoEnabled =>  logWriter.IsInfoEnabled;

        public bool IsWarnEnabled =>  logWriter.IsWarnEnabled;

        public bool IsErrorEnabled =>  logWriter.IsErrorEnabled;
    }
}