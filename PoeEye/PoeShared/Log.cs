using ILog = Common.Logging.ILog;
using LogManager = Common.Logging.LogManager;

namespace PoeShared
{
    using System;

    public static class Log
    {
        private static readonly Lazy<ILog> instance = new Lazy<ILog>(() => LogManager.GetLogger(typeof(Log))); 

        public static ILog Instance => instance.Value;
    }
}