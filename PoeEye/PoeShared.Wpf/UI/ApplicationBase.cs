﻿using System;
using System.Diagnostics;
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
using PoeShared.Logging;
using PoeShared.Services;
using PoeShared.Wpf.Scaffolding;
using ReactiveUI;
using SevenZip;
using Unity;

namespace PoeShared.UI
{
    public abstract class ApplicationBase : Application
    {
        private readonly IAppArguments appArguments;

        protected ApplicationBase()
        {
            try
            {
                Container = new UnityContainer();
                Container.AddNewExtensionIfNotExists<Diagnostic>();
                Container.AddNewExtensionIfNotExists<WpfCommonRegistrations>();
                Container.AddNewExtensionIfNotExists<NativeRegistrations>();
                Container.AddNewExtensionIfNotExists<CommonRegistrations>();

                var arguments = Environment.GetCommandLineArgs();
                appArguments = Container.Resolve<IAppArguments>();
                if (!appArguments.Parse(arguments))
                {
                    SharedLog.Instance.InitializeLogging("Startup", appArguments.AppName);
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }
                InitializeLogging();
                InitializeSevenZip();

                Log.Debug($"Arguments: {arguments.DumpToString()}");
                Log.Debug($"Parsed args: {appArguments.DumpToText()}");
                Log.Debug($"OS: { new { Environment.OSVersion, Environment.Is64BitProcess, Environment.Is64BitOperatingSystem }})");
                Log.Debug($"Environment: {new { Environment.MachineName, Environment.UserName, Environment.WorkingSet, Environment.SystemDirectory, Environment.UserInteractive }})");
                Log.Debug($"Runtime: {new { System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, System.Runtime.InteropServices.RuntimeInformation.OSDescription, OSVersion = Environment.OSVersion.Version }}");
                Log.Debug($"Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
                Log.Debug($"Is Elevated: {appArguments.IsElevated}");
                
                Log.Debug($"UI Scheduler: {RxApp.MainThreadScheduler}");
                RxApp.MainThreadScheduler = Container.Resolve<IScheduler>(WellKnownSchedulers.UI);
                RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Log.Debug($"New UI Scheduler: {RxApp.MainThreadScheduler}");
                Log.Debug($"BG Scheduler: {RxApp.TaskpoolScheduler}");
                
                Log.Debug("Initializing housekeeping");
                var cleanupService = Container.Resolve<IFolderCleanerService>();
                cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "logs"))).AddTo(Anchors);
                cleanupService.CleanupTimeout = TimeSpan.FromHours(12);
                cleanupService.FileTimeToLive = TimeSpan.FromDays(14);
                
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
            catch (Exception ex)
            {
                Log.Error("Unhandled application exception", ex);
                throw;
            }
        }

        public IUnityContainer Container { get; }

        public CompositeDisposable Anchors { get; } = new();

        private static ILog Log => SharedLog.Instance.Log;

        private void InitializeSevenZip()
        {
            Log.Debug($"Initializing 7z wrapper, {nameof(Environment.Is64BitProcess)}: {Environment.Is64BitProcess}");
            var sevenZipDllPath = Path.Combine(appArguments.ApplicationDirectory.FullName, Environment.Is64BitProcess ? "x64" : "x86", "7z.dll");
            Log.Debug($"Setting 7z library path to {sevenZipDllPath}");
            if (!File.Exists(sevenZipDllPath))
            {
                throw new FileNotFoundException("7z library not found", sevenZipDllPath);
            }

            SevenZipBase.SetLibraryPath(sevenZipDllPath);
        }

        private void InitializeLogging()
        {
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
            Container.Resolve<ExceptionReportingService>().AddTo(Anchors);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Log.Debug("Application exit detected");
            base.OnExit(e);
            Anchors.Dispose();
        }
    }
}