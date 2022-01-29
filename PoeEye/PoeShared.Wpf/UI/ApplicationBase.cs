using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Windows;
using App.Metrics;
using PInvoke;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;
using ReactiveUI;
using Unity;
using Unity.Lifetime;

namespace PoeShared.UI;

public abstract class ApplicationBase : Application
{
    private readonly IAppArguments appArguments;
    private readonly IMetricsRoot metrics;

    protected ApplicationBase()
    {
        try
        {
            Container = new UnityContainer();
            Container.RegisterInstance<Application>(this, new ContainerControlledLifetimeManager());
            Container.AddNewExtensionIfNotExists<Diagnostic>();
            Container.AddNewExtensionIfNotExists<CommonRegistrations>();
            Container.AddNewExtensionIfNotExists<NativeRegistrations>();
            Container.AddNewExtensionIfNotExists<WpfCommonRegistrations>();

            appArguments = Container.Resolve<IAppArguments>();
            metrics = Container.Resolve<IMetricsRoot>();
            InitializeLogging();
            Log.Debug(() => $"AppDomain: { new { AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.BaseDirectory,  AppDomain.CurrentDomain.DynamicDirectory }})");
            Log.Debug(() => $"Assemblies: { new { Entry = Assembly.GetEntryAssembly(), Executing = Assembly.GetExecutingAssembly(), Calling = Assembly.GetCallingAssembly() }})");
            Log.Debug(() => $"OS: { new { Environment.OSVersion, Environment.Is64BitProcess, Environment.Is64BitOperatingSystem }})");
            Log.Debug(() => $"Environment: {new { Environment.MachineName, Environment.UserName, Environment.WorkingSet, Environment.SystemDirectory, Environment.UserInteractive }})");
            Log.Debug(() => $"Runtime: {new { System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, System.Runtime.InteropServices.RuntimeInformation.OSDescription, OSVersion = Environment.OSVersion.Version }}");
            Log.Debug(() => $"Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
            Log.Debug(() => $"Is Elevated: {appArguments.IsElevated}");
                
            Log.Debug(() => $"UI Scheduler: {RxApp.MainThreadScheduler}");
            RxApp.MainThreadScheduler = Container.Resolve<IScheduler>(WellKnownSchedulers.UI);
            RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Log.Debug(() => $"New UI Scheduler: {RxApp.MainThreadScheduler}");
            Log.Debug(() => $"BG Scheduler: {RxApp.TaskpoolScheduler}");
                
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
            Log.Debug(() => $"ThreadPool: worker [{minWorkerThreads}; {maxWorkerThreads}], completionPort [{minCompletionPortThreads}; {maxCompletionPortThreads}]");
                
            Log.Debug("Initializing housekeeping");
            var cleanupService = Container.Resolve<IFolderCleanerService>();
            cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "logs"))).AddTo(Anchors);
            cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "crashes"))).AddTo(Anchors);
            cleanupService.CleanupTimeout = TimeSpan.FromHours(12);
            cleanupService.FileTimeToLive = TimeSpan.FromDays(14);
                
            Log.Debug(() => $"Trying to configure DpiAwareness, OS version: {Environment.OSVersion}");
            if (UnsafeNative.IsWindows10OrGreater())
            {
                PInvoke.SHCore.GetProcessDpiAwareness(IntPtr.Zero, out var dpiAwareness);
                Log.Debug(() => $"DpiAwareness: {dpiAwareness}");
                if (dpiAwareness != PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE)
                {
                    Log.Debug(() => $"Setting DpiAwareness of current process {dpiAwareness} => {PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE}");
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
                
            Log.Debug("Configuring process priority");
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception e)
            {
                Log.Warn("Failed to upgrade process priority class", e);
            }
                
            Log.Debug(() => $"Configuring AllowSetForegroundWindow permissions");
            UnsafeNative.AllowSetForegroundWindow();
        }
        catch (Exception ex)
        {
            Log.Error("Unhandled application exception", ex);
            throw;
        }
    }

    public IUnityContainer Container { get; }

    public CompositeDisposable Anchors { get; } = new();

    protected static IFluentLog Log => SharedLog.Instance.Log;

    private void InitializeLogging()
    {
        using var executionTimer = metrics.Measure.Gauge.Time(nameof(InitializeLogging));
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
        SharedLog.Instance.AddTraceAppender().AddTo(Anchors);
        Container.Resolve<IExceptionReportingService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Debug(() => $"Application exit detected, exit code: {e.ApplicationExitCode}");
        base.OnExit(e);
        Log.Debug("Disposing application resources");
        Anchors.Dispose();
        Log.Debug("Disposed application resources");
    }

    public override string ToString()
    {
        return $"App#{Environment.ProcessId}";
    }
}