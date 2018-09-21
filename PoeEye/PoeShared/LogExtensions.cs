using System;
using Common.Logging;
using Guards;
using JetBrains.Annotations;

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
        }
    }
}