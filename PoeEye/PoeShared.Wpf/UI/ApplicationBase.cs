using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows;
using App.Metrics;
using CommandLine;
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

    static ApplicationBase()
    {
        Launcher.Program.HandleStartup();
    }

    protected ApplicationBase() : this(new ApplicationStartupProperties())
    {
    }
    
    protected ApplicationBase(ApplicationStartupProperties startupProperties)
    {
        Log = GetType().PrepareLogger().WithSuffix(ToString);
        try
        {
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                var log = Log.WithSuffix("DomainUnload");
                log.Debug($"[App.DomainUnload] Detected DomainUnload");
                if (Anchors.IsDisposed)
                {
                    log.Debug($"Application anchors are already disposed");
                }
                else
                {
                    log.Debug($"Disposing application anchors");
                    Anchors.Dispose();
                    log.Debug($"Disposed application anchors");
                }
            };

            Container = new UnityContainer();
            Container.RegisterInstance<Application>(this, new ContainerControlledLifetimeManager());
            Container.AddNewExtensionIfNotExists<Diagnostic>();
            Container.AddNewExtensionIfNotExists<CommonRegistrations>();
            Container.AddNewExtensionIfNotExists<NativeRegistrations>();
            Container.AddNewExtensionIfNotExists<HttpClientRegistrations>();
            Container.AddNewExtensionIfNotExists<WpfCommonRegistrations>();

            appArguments = Container.Resolve<IAppArguments>();
            if (!startupProperties.SkipInitialization)
            {
                Initialize(startupProperties);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Unhandled application exception", ex);
            throw;
        }
    }

    public IUnityContainer Container { get; }

    public CompositeDisposable Anchors { get; } = new();

    protected IFluentLog Log { get; }

    protected void Initialize(ApplicationStartupProperties startupProperties)
    {
            if (!startupProperties.DoNotLoadLogConfigFromFile)
            {
                InitializeLogging();
                InitializeLoggingFromFile();
            }
            
            var erService = Container.Resolve<IExceptionReportingService>();
            Log.Debug($"Error reporting service: {erService}");

            var metrics = Container.Resolve<IMetricsRoot>();
            Log.Debug($"UI Metrics: {metrics}");
            
            var applicationAccessor = Container.Resolve<IApplicationAccessor>();
            Log.Info($"Last run state: {new {applicationAccessor.LastLoadWasSuccessful, applicationAccessor.LastExitWasGraceful}}");
            applicationAccessor.WhenExit.Subscribe(OnExit).AddTo(Anchors);


            Log.Debug($"UI Scheduler: {RxApp.MainThreadScheduler}");
            RxApp.MainThreadScheduler = Container.Resolve<IScheduler>(WellKnownSchedulers.UI);
            RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Log.Debug($"New UI Scheduler: {RxApp.MainThreadScheduler}");
            Log.Debug($"BG Scheduler: {RxApp.TaskpoolScheduler}");

            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
            Log.Debug($"ThreadPool: worker [{minWorkerThreads}; {maxWorkerThreads}], completionPort [{minCompletionPortThreads}; {maxCompletionPortThreads}]");

            Log.Debug("Initializing housekeeping");
            var cleanupService = Container.Resolve<IFolderCleanerService>();
            cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.RoamingAppDataDirectory, "logs"))).AddTo(Anchors);
            cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "logs"))).AddTo(Anchors);
            cleanupService.CleanupTimeout = TimeSpan.FromHours(12);
            cleanupService.FileTimeToLive = TimeSpan.FromDays(14);

            Log.Debug($"Trying to configure DpiAwareness, OS version: {Environment.OSVersion}");
            if (UnsafeNative.IsWindows10OrGreater())
            {
                PInvoke.SHCore.GetProcessDpiAwareness(IntPtr.Zero, out var dpiAwareness);
                Log.Debug($"DpiAwareness: {dpiAwareness}");
                //This process checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes. These processes are not automatically scaled by the system.
                if (dpiAwareness != PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE)
                {
                    Log.Debug($"Setting DpiAwareness of current process {dpiAwareness} => {PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE}");
                    if (SHCore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE).Failed)
                    {
                        Log.Warn($"Failed to set DpiAwareness of current process");
                    }
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

            Log.Debug($"Configuring AllowSetForegroundWindow permissions");
            UnsafeNative.AllowSetForegroundWindow();
    }

    protected void InitializeLoggingToConsole()
    {
        InitializeLogging();
        SharedLog.Instance.AddConsoleAppender();

    }
    protected void InitializeLoggingFromFile()
    {
        Log.Debug("Attempting to load configuration from file");
        InitializeLogging();
        var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"log4net.{appArguments.AppName}.config"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config")
            }.Select(x => new FileInfo(x))
            .Select(x => new {LogFile = x, x.Exists})
            .ToArray();
        Log.Debug($"Log files: {candidates.DumpToString()}");
        var logFileConfigPath = candidates.FirstOrDefault(x => x.Exists);
        if (logFileConfigPath?.LogFile != null)
        {
            SharedLog.Instance.LoadLogConfiguration(appArguments, logFileConfigPath.LogFile);
        }
    }

    private void InitializeLogging()
    {
        RxApp.DefaultExceptionHandler = SharedLog.Instance.Errors;
        SharedLog.Instance.InitializeLogging(appArguments);
        SharedLog.Instance.AddTraceAppender().AddTo(Anchors);
    }

    private void OnExit(int exitCode)
    {
        Log.Debug($"Processing application OnExit, exit code: {exitCode}");
        Log.Debug("Disposing application resources");
        Anchors.Dispose();
        Log.Debug("Disposed application resources");
    }

    public override string ToString()
    {
        return $"App#{Environment.ProcessId}";
    }
}