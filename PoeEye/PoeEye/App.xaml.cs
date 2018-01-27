using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using Exceptionless;
using Exceptionless.Models;
using ExceptionReporting;
using ExceptionReporting.Core;
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
                Log.Instance.Debug($"[App..ctor] Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
                
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

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportCrash(e.ExceptionObject as Exception, "CurrentDomainUnhandledException");
        }
        
        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "DispatcherUnhandledException");
        }
        
        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
        }
        
        private void ReportCrash(Exception exception, string developerMessage = "")
        {
            Log.Instance.Error($"Unhandled application exception({developerMessage})", exception);

            try
            {
                var reporter = new ExceptionReporter();

                // otherwise, set configuration via code
                var appName = Assembly.GetExecutingAssembly().GetName().Name;
                reporter.Config.AppName = appName;
                reporter.Config.TitleText = $"{appName} Error Report";
                reporter.Config.MailMethod = ExceptionReportInfo.EmailMethod.SMTP;
                reporter.Config.ShowSysInfoTab = true;
                reporter.Config.ShowFlatButtons = true;
                reporter.Config.ShowContactTab = true;
                reporter.Config.TakeScreenshot = false;
                
                reporter.Config.SmtpFromAddress = $"[E] {Environment.UserName} @ {Environment.MachineName}";
                reporter.Config.EmailReportAddress = AppArguments.PoeEyeMail;
                reporter.Config.ContactEmail = AppArguments.PoeEyeMail;
                reporter.Config.SmtpServer = "aspmx.l.google.com";
                reporter.Config.SmtpPort = 25;
                reporter.Config.SmtpUseSsl = true;
                
                reporter.Config.MainException = exception;

                var configurationFilesToInclude = Directory
                    .EnumerateFiles(AppArguments.AppDataDirectory, "*.cfg", SearchOption.TopDirectoryOnly);

                var logFilesToInclude = new DirectoryInfo(AppArguments.AppDataDirectory)
                    .GetFiles("*.log", SearchOption.AllDirectories)
                    .OrderByDescending(x => x.LastWriteTime)
                    .Take(2)
                    .Select(x => x.FullName)
                    .ToArray();

                reporter.Config.FilesToAttach = new[]
                {
                    logFilesToInclude,
                    configurationFilesToInclude,
                }.SelectMany(x => x).ToArray();
                reporter.Show(exception);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Exception in ExceptionReporter :-(", ex);
            }
        }

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.Dispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            
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
        }

        protected override void OnExit(ExitEventArgs e, bool isFirstInstance)
        {
            base.OnExit(e, isFirstInstance);
            
            Log.Instance.Debug($"Application exit detected");
            bootstrapper.Dispose();
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