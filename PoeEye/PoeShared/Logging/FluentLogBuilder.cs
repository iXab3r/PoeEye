using System;

namespace PoeShared.Logging
{
    internal sealed class FluentLogBuilder : IFluentLog
    {
        private readonly LogData logData;
        private readonly ILogWriter logWriter;

        public FluentLogBuilder(ILogWriter logWriter, LogData logData = new LogData())
        {
            this.logWriter = logWriter;
            this.logData = logData;
        }

        public bool IsDebugEnabled => logWriter.IsDebugEnabled;

        public bool IsInfoEnabled => logWriter.IsInfoEnabled;

        public bool IsWarnEnabled => logWriter.IsWarnEnabled;

        public bool IsErrorEnabled => logWriter.IsErrorEnabled;

        LogData IFluentLog.Data => logData;

        IFluentLog IFluentLog.WithLogData(LogData newLogData)
        {
            return new FluentLogBuilder(logWriter, newLogData);
        }

        public void Debug(string message)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            Debug(message, default);
        }

        public void Debug(string message, Exception exception)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            var newLogData = logData;
            newLogData.LogLevel = FluentLogLevel.Debug;
            newLogData.Message = message;
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Info(string message)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            Info(message, default);
        }

        public void Info(string message, Exception exception)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            var newLogData = logData;
            newLogData.LogLevel = FluentLogLevel.Info;
            newLogData.Message = message;
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Warn(string message)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            Warn(message, default);
        }

        public void Warn(string message, Exception exception)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            var newLogData = logData;
            newLogData.LogLevel = FluentLogLevel.Warn;
            newLogData.Message = message;
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Error(string message)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            Error(message, default);
        }

        public void Error(string message, Exception exception)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            var newLogData = logData;
            newLogData.LogLevel = FluentLogLevel.Error;
            newLogData.Message = message;
            newLogData.Exception = exception;
            logWriter.WriteLog(newLogData);
        }

        public void Debug(Func<string> message)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            Debug(message, default);
        }

        public void Debug(Func<string> message, Exception exception)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            Debug(message(), exception);
        }

        public void Info(Func<string> message)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            Info(message, default);
        }

        public void Info(Func<string> message, Exception exception)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            Info(message(), exception);
        }

        public void Warn(Func<string> message)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            Warn(message, default);
        }

        public void Warn(Func<string> message, Exception exception)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            Warn(message(), default);
        }

        public void Error(Func<string> message)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            Error(message, default);
        }

        public void Error(Func<string> message, Exception exception)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            Error(message(), default);
        }

        public void Debug(FormattableString message)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            Debug(message.ToString);
        }

        public void Debug(FormattableString message, Exception exception)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            Debug(message.ToString, exception);
        }

        public void Info(FormattableString message)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            Info(message.ToString);
        }

        public void Info(FormattableString message, Exception exception)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            Info(message.ToString);
        }

        public void Warn(FormattableString message)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            Warn(message.ToString);
        }

        public void Warn(FormattableString message, Exception exception)
        {
            if (!IsWarnEnabled)
            {
                return;
            }

            Warn(message.ToString);
        }

        public void Error(FormattableString message)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            Error(message.ToString);
        }

        public void Error(FormattableString message, Exception exception)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            Error(message.ToString);
        }
    }
}