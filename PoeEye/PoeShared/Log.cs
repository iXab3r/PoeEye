using ILog = Common.Logging.ILog;
using LogManager = Common.Logging.LogManager;

namespace PoeShared
{
    using System;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    public static class Log
    {
        private static readonly Lazy<ILog> instance = new Lazy<ILog>(() => LogManager.GetLogger(typeof (Log)));

        public static ILog Instance => instance.Value;

        public static void HandleException([NotNull] Exception exception)
        {
            Guard.ArgumentNotNull(() => exception);

            Instance.Error("Exception occurred", exception);
            exception.ToExceptionless().Submit();
        }

        public static void HandleUiException([NotNull] Exception exception)
        {
            Guard.ArgumentNotNull(() => exception);

            Instance.Error("UI Exception occurred", exception);
            exception.ToExceptionless().MarkAsCritical().Submit();
        }
    }
}