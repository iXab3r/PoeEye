﻿using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using Guards;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using PoeShared.Scaffolding;

namespace PoeShared
{
    public class SharedLog : DisposableReactiveObject
    {
        /// <summary>
        ///     Log Instance HAVE to be initialized only after GlobalContext is configured
        /// </summary>
        private static readonly Lazy<ILog> LogInstanceSupplier = new Lazy<ILog>(() =>
        {
            var log = LogManager.GetLogger(typeof(SharedLog));
            log.Debug($"Logger instance initialized, context: {GlobalContext.Properties.DumpToTextRaw()}");
            return log;
        });

        private static readonly Lazy<SharedLog> InstanceSupplier = new Lazy<SharedLog>();

        public SharedLog()
        {
            Errors.Subscribe(ex => { Log.HandleException(ex); }).AddTo(Anchors);
        }

        public ISubject<Exception> Errors { get; } = new Subject<Exception>();

        public static SharedLog Instance => InstanceSupplier.Value;

        public ILog Log => LogInstanceSupplier.Value;

        public void InitializeLogging(string configurationMode)
        {
            InitializeLogging(configurationMode, "PoeSharedUnknownApp");
        }

        public void LoadLogConfiguration(FileInfo logConfig)
        {
            Guard.ArgumentNotNull(logConfig, nameof(logConfig));
            Guard.ArgumentIsTrue(() => logConfig.Exists);

            var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, logConfig);
            Log.Info($"Logging settings loaded from {logConfig}");
        }

        public void InitializeLogging(string configurationMode, string appName)
        {
            Guard.ArgumentNotNull(configurationMode, nameof(configurationMode));

            GlobalContext.Properties["configuration"] = configurationMode;
            GlobalContext.Properties["CONFIGURATION"] = configurationMode;
            GlobalContext.Properties["APPNAME"] = appName;
            Log.Info($"Logging for app {GlobalContext.Properties["APPNAME"]} in '{GlobalContext.Properties["CONFIGURATION"]}' mode initialized");
        }

        public void SwitchLoggingLevel(Level loggingLevel)
        {
            Guard.ArgumentNotNull(loggingLevel, nameof(loggingLevel));

            var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
            repository.Root.Level = loggingLevel;
            repository.RaiseConfigurationChanged(EventArgs.Empty);
            Log.Info($"Logging level switched to '{loggingLevel}'");
        }

        public IDisposable AddAppender(IAppender appender)
        {
            Guard.ArgumentNotNull(appender, nameof(appender));

            var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
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