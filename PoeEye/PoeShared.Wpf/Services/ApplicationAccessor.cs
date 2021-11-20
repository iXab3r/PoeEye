using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using ReactiveUI;

namespace PoeShared.Services
{
    internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
    {
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();
        private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger();
        private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(5);
        private readonly IAppArguments appArguments;
        private readonly Application application;

        public ApplicationAccessor(
            Application application,
            IAppArguments appArguments)
        {
            this.application = application;
            this.appArguments = appArguments;
            Log.Debug($"Initializing Application accessor for {application}");
            if (application == null)
            {
                throw new ApplicationException("Application is not initialized");
            }
            
            Log.Debug($"Binding to application {application}");
            WhenExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => application.Exit += h, h => application.Exit -= h)
                .Select(x => x.EventArgs.ApplicationExitCode)
                .Replay(1)
                .AutoConnect();
            WhenExit.SubscribeSafe(x =>
            {
                Log.Info($"Application exit requested, exit code: {x}");
                IsExiting = true;
            }, Log.HandleException).AddTo(Anchors);
            LastExitWasGraceful = InitializeRunningLockFile();
            LastLoadWasSuccessful = InitializeLoadingLockFile();
            Disposable.Create(() => Log.Debug("Disposed")).AddTo(Anchors);
        }

        public bool IsLoaded { get; private set; }
        
        public bool LastExitWasGraceful { get; }
        
        public bool LastLoadWasSuccessful { get; }
        
        public void ReportIsLoaded()
        {
            if (IsLoaded)
            {
                throw new InvalidOperationException("Application is already loaded");
            }

            Log.Debug("Marking application as loaded");
            IsLoaded = true;
        }

        public IObservable<int> WhenExit { get; }

        public bool IsExiting { get; private set; }

        public void Terminate(int exitCode)
        {
            Log.Warn($"Closing application via Environment.Exit with code {exitCode}");
            Environment.Exit(exitCode);
        }

        public async Task Exit()
        {
            Log.Debug($"Attempting to gracefully shutdown application, IsExiting: {IsExiting}");
            lock (application)
            {
                if (IsExiting)
                {
                    Log.Warn("Shutdown is already in progress");
                    return;
                }
                IsExiting = true;
            }

            try
            {
                Shutdown();

                Log.Info($"Awaiting for application termination for {TerminationTimeout}");
                var exitCode = await WhenExit.Take(1).Timeout(TerminationTimeout);
                
                Log.Info($"Application termination signal was processed, exit code: {exitCode}");

                await Task.Delay(TerminationTimeout);
                Log.Warn($"Application should've terminated by now");
                throw new ApplicationException("Something went wrong - application failed to Shutdown gracefully");
            }
            catch (Exception e)
            {
                Log.Warn("Failed to terminate app gracefully, forcing Terminate", e);
                Terminate(-1);
            }
        }

        private void Shutdown()
        {
            if (!application.CheckAccess())
            {
                throw new InvalidOperationException($"{nameof(Shutdown)} invoked on non-main thread");
            }
            
            Log.Debug($"Terminating application (shutdownMode: {application.ShutdownMode}, window: {application.MainWindow})...");
            if (application.MainWindow != null && application.ShutdownMode == ShutdownMode.OnMainWindowClose)
            {
                Log.Debug($"Closing main window {application.MainWindow}...");
                application.MainWindow.Close();
            }
            else
            {
                Log.Debug($"Closing app via Shutdown");
                application.Shutdown(0);
            }
        }

        private bool InitializeLoadingLockFile()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $".loading{(appArguments.IsDebugMode ? null : $"DebugMode")}");
            var fileExists = File.Exists(filePath);
            Log.Debug($"Application .loading lock file path: {filePath}, exists: {LastLoadWasSuccessful}");
            PrepareLockFile(filePath);
            this.WhenAnyValue(x => x.IsLoaded).Where(x => x == true).SubscribeSafe(x =>
            {
                Log.Debug($"Application is loaded - cleaning up .loading lock file {filePath}");
                CleanupLockFile(filePath);
            }, Log.HandleException).AddTo(Anchors);
            return !fileExists;
        }

        private bool InitializeRunningLockFile()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $".running{(appArguments.IsDebugMode ? null : $"DebugMode")}");
            var fileExists = File.Exists(filePath);
            Log.Debug($"Application .running lock file path: {filePath}, exists: {fileExists}");
            if (!LastExitWasGraceful)
            {
                Log.Warn("Seems that last application start was not graceful - lock file is still present");
            }
            PrepareLockFile(filePath);
            WhenExit.SubscribeSafe(exitCode =>
            {
                if (exitCode == 0)
                {
                    Log.Debug("Graceful exit - cleaning up lock file");
                    CleanupLockFile(filePath);
                }
                else
                {
                    Log.Warn($"Erroneous exit detected, code: {exitCode} - leaving lock file intact @ {filePath}");
                }
            }, Log.HandleException).AddTo(Anchors);
            return !fileExists;
        }

        private static void PrepareLockFile(string lockFilePath)
        {
            Log.Debug($"Creating lock file: {lockFilePath}");
            var lockFileData = $"pid: {CurrentProcess.Id}, start time: {CurrentProcess.StartTime}, args: {UnsafeNative.GetCommandLine(CurrentProcess.Id)}";
            using var lockFileStream = new StreamWriter(new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
            Log.Debug($"Filling lock file with data: {lockFileData}");
            lockFileStream.Write(lockFileData);
        }

        private static void CleanupLockFile(string lockFilePath)
        {
            Log.Debug($"Preparing to remove lock file: {lockFilePath}");
            var lockFileExists = File.Exists(lockFilePath);
            if (lockFileExists)
            {
                Log.Warn($"Removing lock file {lockFilePath}");
                File.Delete(lockFilePath);
            }
            else
            {
                Log.Warn($"Lock file {lockFilePath} does not exist for some reason");
            }
            
            if (File.Exists(lockFilePath))
            {
                throw new ApplicationException($"Failed to remove lock file");
            }
        }
    }
}