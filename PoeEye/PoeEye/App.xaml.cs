using PoeShared.Scaffolding;

namespace PoeEye
{
    using System;
    using System.Reflection;
    using System.Windows;

    using Exceptionless;
    using Exceptionless.Models;

    using log4net;
    using log4net.Core;

    using PoeShared;

    using Prism;

    using ReactiveUI;

    public partial class App
    {
        private static readonly string AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

        public static readonly AppArguments Arguments = new AppArguments();

        public App()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (!CommandLine.Parser.Default.ParseArguments(arguments, Arguments))
            {
                throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
            }

            InitializeLogging();
            Log.Instance.Debug($"[App..ctor] Arguments: {arguments.DumpToText()}");
            Log.Instance.Debug($"[App..ctor] Parsed args: {Arguments.DumpToText()}");

            InitializeExceptionless();

            RxApp.SupportsRangeNotifications = true;

        }

        private static void InitializeExceptionless()
        {
            Log.Instance.Debug("Initializing exceptionless...");
            ExceptionlessClient.Default.Configuration.ApiKey = "dkjcxnVxQO9Nx6zJdYYyAW66gHt5YP5XCmHNmjYj";
            ExceptionlessClient.Default.Configuration.DefaultTags.Add($".NET {Environment.Version}");
            ExceptionlessClient.Default.Configuration.DefaultTags.Add($"OS:{Environment.OSVersion}");
            ExceptionlessClient.Default.Configuration.DefaultTags.Add(AppVersion);
            ExceptionlessClient.Default.Configuration.IncludePrivateInformation = true;
            ExceptionlessClient.Default.Configuration.SetVersion(AppVersion);
            ExceptionlessClient.Default.Configuration.SetUserIdentity($"{Environment.UserName}@{Environment.MachineName}");

            ExceptionlessClient.Default.SubmitEvent(new Event { Message = AppVersion, Type = "Version" });
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Log.Instance.Error($"Unhandled application exception", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }

        private void InitializeLogging()
        {
            if (Arguments.IsDebugMode)
            {
                GlobalContext.Properties["configuration"] = "Debug";

                var repository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                repository.Root.Level = Level.Trace;
                repository.RaiseConfigurationChanged(EventArgs.Empty);

                Log.Instance.Info("Debug mode initialized");
            }
            else
            {
                GlobalContext.Properties["configuration"] = "Release";
                Log.Instance.Info("Release mode initialized");
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Log.Instance.Info("Application logging started");
        }

        protected override void OnStartup(StartupEventArgs e, bool? isFirstInstance)
        {
            base.OnStartup(e, isFirstInstance);

            if (isFirstInstance != true)
            {
                Log.Instance.Warn($"Application is already running !");
                ShutdownIfNotInDebugMode();
            }

            Log.Instance.Info($"Initializing bootstrapper...");
            var bootstrapper = new PoeEyeBootstrapper();
            bootstrapper.Run();
        }

        private void ShutdownIfNotInDebugMode()
        {
#if !DEBUG
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var window = MainWindow;
            var title = $"{assemblyName.Name} v{assemblyName.Version}";
            var message = "Application is already running !";
            if (window != null)
            {
                MessageBox.Show(window, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            Log.Instance.Warn($"Shutting down...");
            Shutdown(1);
#endif
        }
    }
}