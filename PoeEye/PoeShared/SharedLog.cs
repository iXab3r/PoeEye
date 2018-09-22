using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Guards;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using PoeShared.Scaffolding;
using ILog = Common.Logging.ILog;
using LogManager = Common.Logging.LogManager;

namespace PoeShared
{
    public class SharedLog : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharedLog));

        private static readonly Lazy<SharedLog> InstanceSupplier = new Lazy<SharedLog>();

        public SharedLog()
        {
            Errors.Subscribe(Log.HandleUiException).AddTo(Anchors);
        }

        public ISubject<Exception> Errors { get; } = new Subject<Exception>();

        public static SharedLog Instance => InstanceSupplier.Value;

        public static void InitializeLogging(string configurationMode)
        {
            Guard.ArgumentNotNull(configurationMode, nameof(configurationMode));

            GlobalContext.Properties["configuration"] = configurationMode;
            Log.Info($"Logging in '{configurationMode}' mode initialized");
        }

        public static void SwitchLoggingLevel(Level loggingLevel)
        {
            Guard.ArgumentNotNull(loggingLevel, nameof(loggingLevel));

            var repository = (Hierarchy)log4net.LogManager.GetRepository();
            repository.Root.Level = loggingLevel;
            repository.RaiseConfigurationChanged(EventArgs.Empty);
            Log.Info($"Logging level switched to '{loggingLevel}'");
        }
        
        public static IDisposable AddAppender(IAppender appender)
        {
            Guard.ArgumentNotNull(appender, nameof(appender));

            var repository = (Hierarchy)log4net.LogManager.GetRepository();
            var root = repository.Root;
            Log.Debug($"Adding appender {appender}, currently root contains {root.Appenders.Count} appenders");
            root.AddAppender(appender);
            repository.RaiseConfigurationChanged(EventArgs.Empty);
            
            return Disposable.Create(() =>
            {
                Log.Debug($"Removing appender {appender}, currently root contains {root.Appenders.Count} appenders");
                root.RemoveAppender(appender);
                repository.RaiseConfigurationChanged(EventArgs.Empty);
            });
        }
    }
}