using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;
using PoeShared.Squirrel.Core;
using ReactiveUI;
using Squirrel;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class ApplicationUpdaterModel : DisposableReactiveObject, IApplicationUpdaterModel
    {
        private static readonly IFluentLog Log = typeof(ApplicationUpdaterModel).PrepareLogger();

        private static readonly string DotnetCoreRunnerName = "dotnet.exe";
        private static readonly string UpdaterExecutableName = "update.exe";
        private readonly IAppArguments appArguments;
        private readonly IApplicationAccessor applicationAccessor;
        private Version mostRecentVersion;
 

        public ApplicationUpdaterModel(
            IApplicationAccessor applicationAccessor,
            IUpdateSourceProvider UpdateSourceProvider,
            IAppArguments appArguments)
        {
            this.applicationAccessor = applicationAccessor;
            this.appArguments = appArguments;

            MostRecentVersionAppFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            UpdatedVersion = null;
            
            UpdateSourceProvider
                .WhenAnyValue(x => x.UpdateSource)
                .WithPrevious()
                .SubscribeSafe(x =>
                {
                    Log.Debug($"Update source changed: {x}");
                    UpdateSource = x.Current;
                }, Log.HandleUiException)
                .AddTo(Anchors);
            
            var currentProcessName = Process.GetCurrentProcess().ProcessName + ".exe";
            Log.Debug($"Initializing ApplicationName, processName: {currentProcessName}, appArguments executable: {appArguments.ApplicationExecutableName}");
            if (string.Equals(DotnetCoreRunnerName, currentProcessName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug($"Detected that Application is running using .net core runner ({DotnetCoreRunnerName})");

                if (string.IsNullOrEmpty(appArguments.ApplicationExecutableName) || Path.GetExtension(appArguments.ApplicationExecutableName) != ".dll")
                {
                    throw new NotSupportedException("Could not determine application name, expected either .dll with .net runner or raw executable (.exe)");
                }
                
                Log.Debug($"Extracting application executable name from {appArguments.ApplicationExecutableName}");
                var executableName = Path.ChangeExtension(appArguments.ApplicationExecutableName, ".exe");
                ApplicationExecutableFileName = executableName;
            }
            else
            {
                ApplicationExecutableFileName = currentProcessName;
            }
            Log.Debug($"Application will be started via executing {ApplicationExecutableFileName}");
        }

        public string ApplicationExecutableFileName { get; }

        public DirectoryInfo MostRecentVersionAppFolder { get; set; }

        public UpdateSourceInfo UpdateSource { get; set; }

        public bool IgnoreDeltaUpdates { get; set; }

        public Version UpdatedVersion
        {
            get => mostRecentVersion;
            private set => RaiseAndSetIfChanged(ref mostRecentVersion, value);
        }

        public IPoeUpdateInfo LatestVersion { get; private set; }

        public int ProgressPercent { get; private set; }

        public bool IsBusy { get; private set; }

        public async Task ApplyRelease(IPoeUpdateInfo updateInfo)
        {
            Guard.ArgumentNotNull(updateInfo, nameof(updateInfo));

            Log.Debug($"Applying update {updateInfo}");

            using var unused = CreateIsBusyAnchor();
            using var mgr = await CreateManager();
            
            Log.Debug("Downloading releases...");
            await mgr.DownloadReleases(updateInfo.ReleasesToApply, x => UpdateProgress(x, "DownloadRelease"));

            string newVersionFolder;
            if (string.IsNullOrWhiteSpace(GetSquirrelUpdateExe()))
            {
                Log.Warn("Not a Squirrel-app or debug mode detected, skipping update");
                newVersionFolder = AppDomain.CurrentDomain.BaseDirectory;
                for (var i = 0; i < 100; i++)
                {
                    UpdateProgress(i, "Debug");
                    await Task.Delay(5000);
                }
            }
            else
            {
                Log.Debug("Applying releases...");
                newVersionFolder = await mgr.ApplyReleases(updateInfo, x => UpdateProgress(x, "ApplyRelease"));
            }

            var lastAppliedRelease = updateInfo.ReleasesToApply.Last();

            Log.Debug(
                $"Update completed to v{lastAppliedRelease.Version}, result: {newVersionFolder}");

            if (string.IsNullOrWhiteSpace(newVersionFolder))
            {
                throw new ApplicationException("Expected non-empty new version folder path");
            }

            MostRecentVersionAppFolder = new DirectoryInfo(newVersionFolder);
            UpdatedVersion = lastAppliedRelease.Version.Version;
            LatestVersion = null;
            ProgressPercent = 0;
        }

        public void Reset()
        {
            LatestVersion = null;
            UpdatedVersion = null;
            IsBusy = false;
            ProgressPercent = 0;
        }

        public void HandleSquirrelEvents()
        {
            Log.Debug("Handling Squirrel events");
            SquirrelAwareApp.HandleEvents(
                OnInitialInstall,
                OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);
            Log.Debug("Squirrel events were handled successfully");
        }

        /// <summary>
        ///     Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        public async Task<IPoeUpdateInfo> CheckForUpdates()
        {
            Log.Debug("Update check requested");
            using var unused = CreateIsBusyAnchor();
            Reset();

            using var mgr = await CreateManager();
            Log.Debug($"Checking for updates @ {UpdateSource}, {nameof(IgnoreDeltaUpdates)}: {IgnoreDeltaUpdates}...");

            var updateInfo = await mgr.CheckForUpdate(IgnoreDeltaUpdates, CheckUpdateProgress);

            Log.Debug($"UpdateInfo:\r\n{updateInfo?.DumpToText()}");
            if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
            {
                return null;
            }

            LatestVersion = updateInfo;
            return updateInfo;
        }

        public async Task RestartApplication()
        {
            // NB: Here's how this method works:
            //
            // 1. We're going to pass the *name* of our EXE and the params to 
            //    Update.exe
            // 2. Update.exe is going to grab our PID (via getting its parent), 
            //    then wait for us to exit.
            // 3. We exit cleanly, dropping any single-instance mutexes or 
            //    whatever.
            // 4. Update.exe unblocks, then we launch the app again, possibly 
            //    launching a different version than we started with (this is why
            //    we take the app's *name* rather than a full path)
            using var unused = CreateIsBusyAnchor();
            
            var executable = GetLatestExecutable();
            Log.Debug(
                $"Restarting app, folder: {MostRecentVersionAppFolder}, appName: {ApplicationExecutableFileName}, {executable}...");

            var squirrelUpdater = GetSquirrelUpdateExeOrThrow();
            var squirrelArgs = new StringBuilder($"--processStartAndWait {executable.FullName}");
            if (appArguments.IsDebugMode)
            {
                squirrelArgs.Append($" --process-start-args=-d");
            }

            Log.Debug($"Starting Squirrel updater @ '{squirrelUpdater}', args: {squirrelArgs} ...");
            var updaterProcess = Process.Start(squirrelUpdater, squirrelArgs.ToString());
            if (updaterProcess == null)
            {
                throw new FileNotFoundException($"Failed to start updater @ '{squirrelUpdater}'");
            }
            
            //FIXME Rewrite this pile of mess.PID should be send via args along with a mutex name, that way it will be fully controllable
            
            // NB: We have to give update.exe some time to grab our PID, but
            // we can't use WaitForInputIdle because we probably don't have
            // whatever WaitForInputIdle considers a message loop.
            Log.Debug($"Process spawned, PID: {updaterProcess.Id}");
            await Task.Delay(2000);
            await applicationAccessor.Exit();
        }

        public FileInfo GetLatestExecutable()
        {
            var appExecutable = new FileInfo(Path.Combine(MostRecentVersionAppFolder.FullName, ApplicationExecutableFileName));
            Log.Debug($"Most recent version folder: {MostRecentVersionAppFolder}, appName: { ApplicationExecutableFileName}, exePath: {appExecutable}(exists: {appExecutable.Exists})...");

            if (!appExecutable.Exists)
            {
                throw new FileNotFoundException("Application executable was not found", appExecutable.FullName);
            }
            return appExecutable;
        }

        private async Task<PoeUpdateManager> CreateManager()
        {
            var appName = Process.GetCurrentProcess().ProcessName;
            var rootDirectory = default(string);

            if (appArguments.IsDebugMode || string.IsNullOrWhiteSpace(GetSquirrelUpdateExe()))
            {
                rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            Log.Debug($"AppName: {appName}, root directory: {rootDirectory}");

            Log.Debug($"Using update source: {UpdateSource.DumpToTextRaw()}");
            var downloader = new BasicAuthFileDownloader(
                new NetworkCredential(
                    UpdateSource.Username?.ToUnsecuredString(),
                    UpdateSource.Password?.ToUnsecuredString()));
            if (UpdateSource.Uri.Contains("github"))
            {
                Log.Debug($"Using GitHub source: {UpdateSource.DumpToTextRaw()}");

                var mgr = PoeUpdateManager.GitHubUpdateManager(
                    UpdateSource.Uri,
                    downloader,
                    appName,
                    rootDirectory);
                return await mgr;
            }
            else
            {
                Log.Debug($"Using BasicHTTP source: {UpdateSource.DumpToTextRaw()}");
                var mgr = new PoeUpdateManager(UpdateSource.Uri, downloader, appName, rootDirectory);
                return mgr;
            }
        }

        private void UpdateProgress(int progressPercent, string taskName)
        {
            Log.Debug($"{taskName} is in progress: {progressPercent}%");
            ProgressPercent = progressPercent;
        }

        private void CheckUpdateProgress(int progressPercent)
        {
            Log.Debug($"Check update is in progress: {progressPercent}%");
        }

        private void OnAppUninstall(Version appVersion)
        {
            Log.Debug($"Uninstalling v{appVersion}...");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnAppUpdate(Version appVersion)
        {
            Log.Debug($"Updating v{appVersion}...");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnInitialInstall(Version appVersion)
        {
            Log.Debug($"App v{appVersion} installed");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnFirstRun()
        {
            Log.Debug("App started for the first time");
        }

        private static string GetSquirrelUpdateExe()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null &&
                Path.GetFileName(entryAssembly.Location).Equals(UpdaterExecutableName, StringComparison.OrdinalIgnoreCase) &&
                entryAssembly.Location.IndexOf("app-", StringComparison.OrdinalIgnoreCase) == -1 &&
                entryAssembly.Location.IndexOf("SquirrelTemp", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return Path.GetFullPath(entryAssembly.Location);
            }

            var squirrelAssembly = typeof(PoeUpdateManager).Assembly;
            var executingAssembly = Path.GetDirectoryName(squirrelAssembly.Location);
            if (executingAssembly == null)
            {
                throw new ApplicationException($"Failed to get directory assembly {squirrelAssembly}");
            }

            return Path.Combine(executingAssembly, "..", UpdaterExecutableName);
        }

        private static string GetSquirrelUpdateExeOrThrow()
        {
            var squirrelUpdateExe = GetSquirrelUpdateExe();
            if (!File.Exists(squirrelUpdateExe))
            {
                throw new FileNotFoundException($"{UpdaterExecutableName} not found(path: {squirrelUpdateExe}), not a Squirrel-installed app?",
                    squirrelUpdateExe);
            }

            return squirrelUpdateExe;
        }

        private IDisposable CreateIsBusyAnchor()
        {
            IsBusy = true;
            ProgressPercent = 0;

            return Disposable.Create(
                () =>
                {
                    IsBusy = false;
                    ProgressPercent = 0;
                });
        }
    }
}