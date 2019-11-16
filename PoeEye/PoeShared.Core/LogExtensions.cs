using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using log4net;

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
    }
}