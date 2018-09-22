using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Common.Logging;
using ExceptionReporting;
using ExceptionReporting.Core;
using PoeEye.Prism;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye
{
    public partial class App
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        private readonly PoeEyeBootstrapper bootstrapper = new PoeEyeBootstrapper();

        public App()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                if (!AppArguments.Parse(arguments))
                {
                    SharedLog.InitializeLogging("Startup");
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }

                InitializeLogging();
                Log.Debug($"[App..ctor] Arguments: {arguments.DumpToText()}");
                Log.Debug($"[App..ctor] Parsed args: {AppArguments.Instance.DumpToText()}");
                Log.Debug($"[App..ctor] Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");

                RxApp.SupportsRangeNotifications = false; //FIXME DynamicData (as of v4.11) does not support RangeNotifications
                Log.Debug($"[App..ctor] UI Scheduler: {RxApp.MainThreadScheduler}");
                RxApp.MainThreadScheduler = DispatcherScheduler.Current;
                RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
                Log.Debug($"[App..ctor] New UI Scheduler: {RxApp.MainThreadScheduler}");
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                throw;
            }
        }

        private void SingleInstanceValidationRoutine()
        {
            var mutexId = $"PoeEye{(AppArguments.Instance.IsDebugMode ? "DEBUG" : "RELEASE")}{{88286F90-96B8-4799-9E8E-78B581267D63}}";
            Log.Debug($"[App] Acquiring mutex {mutexId}...");
            var mutex = new Mutex(true, mutexId);
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Log.Debug($"[App] Mutex {mutexId} was successfully acquired");

                AppDomain.CurrentDomain.DomainUnload += delegate
                {
                    Log.Debug($"[App.DomainUnload] Detected DomainUnload, disposing mutex {mutexId}");
                    mutex.ReleaseMutex();
                    Log.Debug("[App.DomainUnload] Mutex was successfully disposed");
                };
            }
            else
            {
                Log.Warn($"[App] Appliation is already running, mutex: {mutexId}");
                ShowShutdownWarning();
            }
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
            Log.Error($"Unhandled application exception({developerMessage})", exception);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            Current.Dispatcher.UnhandledException -= DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;

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
                    configurationFilesToInclude
                }.SelectMany(x => x).ToArray();
                reporter.Show(exception);
            }
            catch (Exception ex)
            {
                Log.Error("Exception in ExceptionReporter :-(", ex);
            }
        }

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Current.Dispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            RxApp.DefaultExceptionHandler = SharedLog.Instance.Errors;
            if (AppArguments.Instance.IsDebugMode)
            {
                SharedLog.InitializeLogging("Debug");
            }
            else
            {
                SharedLog.InitializeLogging("Release");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Debug("Application startup detected");

            SingleInstanceValidationRoutine();

            Log.Info("Initializing bootstrapper...");
            bootstrapper.Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Log.Debug("Application exit detected");
            bootstrapper.Dispose();
        }

        private void ShowShutdownWarning()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
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

            Log.Warn("Shutting down...");
            Environment.Exit(0);
        }
    }
}