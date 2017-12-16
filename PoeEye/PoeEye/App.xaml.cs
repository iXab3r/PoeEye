using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using CommandLine;
using Exceptionless;
using Exceptionless.Models;
using Guards;
using log4net.Core;
using Microsoft.Practices.Unity;
using PoeEye.Prism;
using PoeShared;
using PoeShared.Communications.Chromium;
using PoeShared.Scaffolding;
using Prism.Unity;
using ReactiveUI;

namespace PoeEye
{
    public partial class App
    {
        private static readonly string AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

        private readonly PoeEyeBootstrapper bootstrapper = new PoeEyeBootstrapper();

        public App()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                if (!AppArguments.Parse(arguments))
                {
                    Log.InitializeLogging("Startup");
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }

                InitializeLogging();
                Log.Instance.Debug($"[App..ctor] Arguments: {arguments.DumpToText()}");
                Log.Instance.Debug($"[App..ctor] Parsed args: {AppArguments.Instance.DumpToText()}");

                InitializeExceptionless();

                RxApp.SupportsRangeNotifications = false; //FIXME DynamicData (as of v4.11) does not support RangeNotifications
            }
            catch (Exception e)
            {
                Log.HandleException(e);
                throw;
            }
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
            ExceptionlessClient.Default.Configuration.SetUserIdentity(
                $"{Environment.UserName}@{Environment.MachineName}");

            ExceptionlessClient.Default.SubmitEvent(new Event {Message = AppVersion, Type = "Version"});
        }

        private void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Log.Instance.Error(
                $"Unhandled application exception", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            RxApp.DefaultExceptionHandler = Log.ErrorsSubject;
            if (AppArguments.Instance.IsDebugMode)
            {
                Log.InitializeLogging("Debug");
                Log.SwitchLoggingLevel(Level.Debug);
            }
            else
            {
                Log.InitializeLogging("Release");
            }
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
            bootstrapper.Run();
            
            Log.Instance.Info($"Initializing Chromium...");
            var chromium = bootstrapper.Container.Resolve<IChromiumBootstrapper>();
        }

        protected override void OnExit(ExitEventArgs e, bool isFirstInstance)
        {
            base.OnExit(e, isFirstInstance);
            
            Log.Instance.Debug($"Application exit detected");
            var chromium = bootstrapper.Container.TryResolve<IChromiumBootstrapper>();
            chromium?.Dispose();
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