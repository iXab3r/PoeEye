using System;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using PoeShared.Scaffolding;
using ILog = Common.Logging.ILog;
using LogManager = Common.Logging.LogManager;

namespace PoeShared
{
    public class Log : DisposableReactiveObject
    {
        private static readonly Lazy<Log> InstanceProvider = new Lazy<Log>();

        private readonly Lazy<ILog> loggerInstanceProvider = new Lazy<ILog>(() => LogManager.GetLogger(typeof(Log)));

        public Log()
        {
            Errors.Subscribe(HandleUiException).AddTo(Anchors);
        }

        private static ILog Instance => InstanceProvider.Value.Logger;

        public static ISubject<Exception> ErrorsSubject => InstanceProvider.Value.Errors;

        private ISubject<Exception> Errors { get; } = new Subject<Exception>();

        private ILog Logger => loggerInstanceProvider.Value;

        public static void HandleException([NotNull] Exception exception)
        {
            Guard.ArgumentNotNull(exception, nameof(exception));

            Instance.HandleException(exception);
        }

        public static void HandleUiException([NotNull] Exception exception)
        {
            Guard.ArgumentNotNull(exception, nameof(exception));

            Instance.HandleUiException(exception);
        }

        public static void InitializeLogging(string configurationMode)
        {
            Guard.ArgumentNotNull(configurationMode, nameof(configurationMode));

            GlobalContext.Properties["configuration"] = configurationMode;
            Instance.Info($"Logging in '{configurationMode}' mode initialized");
        }

        public static void SwitchLoggingLevel(Level loggingLevel)
        {
            Guard.ArgumentNotNull(loggingLevel, nameof(loggingLevel));

            var repository = (Hierarchy)log4net.LogManager.GetRepository();
            repository.Root.Level = loggingLevel;
            repository.RaiseConfigurationChanged(EventArgs.Empty);
            Instance.Info($"Logging level switched to '{loggingLevel}'");
        }
    }
}