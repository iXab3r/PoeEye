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
    }
}