using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using PInvoke;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Wpf.UI.ExceptionViewer;
using ReactiveUI;
using Unity;

namespace PoeShared.UI
{
    public abstract class ApplicationBase : Application
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IUnityContainer container;
        private readonly IAppArguments appArguments;

        protected ApplicationBase()
        {
            try
            {
                container = new UnityContainer();
                container.AddNewExtensionIfNotExists<Diagnostic>();
                container.AddNewExtensionIfNotExists<WpfCommonRegistrations>();
                container.AddNewExtensionIfNotExists<NativeRegistrations>();
                container.AddNewExtensionIfNotExists<CommonRegistrations>();

                var arguments = Environment.GetCommandLineArgs();
                appArguments = container.Resolve<IAppArguments>();
                if (!appArguments.Parse(arguments))
                {
                    SharedLog.Instance.InitializeLogging("Startup", appArguments.AppName);
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }
                InitializeLogging();

                Log.Debug($"Arguments: {arguments.DumpToText()}");
                Log.Debug($"Parsed args: {appArguments.DumpToText()}");
                Log.Debug($"OS: { new { Environment.OSVersion, Environment.Is64BitProcess, Environment.Is64BitOperatingSystem }})");
                Log.Debug($"Environment: {new { Environment.MachineName, Environment.UserName, Environment.WorkingSet, Environment.SystemDirectory, Environment.UserInteractive }})");
                Log.Debug($"Runtime: {new { System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, System.Runtime.InteropServices.RuntimeInformation.OSDescription, OSVersion = Environment.OSVersion.Version }}");
                Log.Debug($"Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
                Log.Debug($"Is Elevated: {appArguments.IsElevated}");
                
                Log.Debug($"UI Scheduler: {RxApp.MainThreadScheduler}");
                RxApp.MainThreadScheduler = container.Resolve<IScheduler>(WellKnownSchedulers.UI);
                RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Log.Debug($"New UI Scheduler: {RxApp.MainThreadScheduler}");
                Log.Debug($"BG Scheduler: {RxApp.TaskpoolScheduler}");
                
                Log.Debug($"Trying to configure DpiAwareness, OS version: {Environment.OSVersion}");
                if (UnsafeNative.IsWindows10OrGreater())
                {
                    PInvoke.SHCore.GetProcessDpiAwareness(IntPtr.Zero, out var dpiAwareness);
                    Log.Debug($"DpiAwareness: {dpiAwareness}");
                    if (dpiAwareness != PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE)
                    {
                        Log.Debug($"Setting DpiAwareness of current process {dpiAwareness} => {PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE}");
                        if (SHCore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE).Failed)
                        {
                            Log.Warn($"Failed to set DpiAwareness of current process");
                        };
                    }
                }
                else
                {
                    Log.Warn("DpiAwareness is supported only on Windows 10 or greater");
                }
                Log.Debug($"Configuring AllowSetForegroundWindow permissions");
                UnsafeNative.AllowSetForegroundWindow();
            }
            catch (Exception ex)
            {
                ReportCrash(ex);
                throw;
            }
        }

        private static ILog Log => SharedLog.Instance.Log;

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

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            RxApp.DefaultExceptionHandler = SharedLog.Instance.Errors;
            if (appArguments.IsDebugMode)
            {
                SharedLog.Instance.InitializeLogging("Debug", appArguments.AppName);
            }
            else
            {
                SharedLog.Instance.InitializeLogging("Release", appArguments.AppName);
            }

            var logFileConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            SharedLog.Instance.LoadLogConfiguration(new FileInfo(logFileConfigPath));
            SharedLog.Instance.AddTraceAppender().AddTo(anchors);
            SharedLog.Instance.Errors.SubscribeToErrors(
                ex =>
                {
                    ReportCrash(ex);
                }).AddTo(anchors);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Log.Debug("Application exit detected");
            base.OnExit(e);
            anchors.Dispose();
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
        
        private void ReportCrash(Exception exception, string developerMessage = "")
        {
            Log.Error($"Unhandled application exception({developerMessage})", exception);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
            Dispatcher.CurrentDispatcher.UnhandledException -= DispatcherOnUnhandledException;
            
            var reporter = container.Resolve<IExceptionDialogDisplayer>();
            reporter.ShowDialogAndTerminate(exception);
        }
    }
}