using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeEye.Utilities;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;
using Squirrel;

namespace PoeEye.PoeTrade.Updater
{
    internal sealed class ApplicationUpdaterModel : DisposableReactiveObject, IApplicationUpdaterModel
    {
        private static readonly string ApplicationName = Process.GetCurrentProcess().ProcessName + ".exe";

        private readonly IConfigProvider<PoeEyeUpdateSettingsConfig> configProvider;

        private Version mostRecentVersion;
        private DirectoryInfo mostRecentVersionAppFolder;

        public ApplicationUpdaterModel(
            [NotNull] IConfigProvider<PoeEyeUpdateSettingsConfig> configProvider)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            this.configProvider = configProvider;

            SquirrelAwareApp.HandleEvents(
                OnInitialInstall,
                OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);

            MostRecentVersionAppFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            MostRecentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public DirectoryInfo MostRecentVersionAppFolder
        {
            get => mostRecentVersionAppFolder;
            set => this.RaiseAndSetIfChanged(ref mostRecentVersionAppFolder, value);
        }

        public Version MostRecentVersion
        {
            get => mostRecentVersion;
            set => this.RaiseAndSetIfChanged(ref mostRecentVersion, value);
        }

        /// <summary>
        ///     Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        public async Task<bool> CheckForUpdates()
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel] Update check requested");

            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            var rootDirectory = default(string);

            if (AppArguments.Instance.IsDebugMode)
            {
                rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            Log.Instance.Debug($"[ApplicationUpdaterModel] AppName: {appName}, root directory: {rootDirectory}");

            var updateSource = configProvider.ActualConfig.UpdateSource;
            Log.Instance.Debug($"[ApplicationUpdaterModel] Using update source: {updateSource}");
            var downloader =
                new BasicAuthFileDownloader(new NetworkCredential(configProvider.ActualConfig.UpdateSource.Username,
                                                                  configProvider.ActualConfig.UpdateSource.Password));

            using (var mgr = new UpdateManager(updateSource.Uri, appName, rootDirectory, downloader))
            {
                Log.Instance.Debug($"[ApplicationUpdaterModel] Checking for updates...");

                var updateInfo = await mgr.CheckForUpdate(true, CheckUpdateProgress);

                Log.Instance.Debug($"[ApplicationUpdaterModel] UpdateInfo:\r\n{updateInfo?.DumpToText()}");
                if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
                {
                    return false;
                }

                Log.Instance.Debug($"[ApplicationUpdaterModel] Downloading releases...");
                await mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress);

                string newVersionFolder;
                if (AppArguments.Instance.IsDebugMode)
                {
                    Log.Instance.Debug("[ApplicationUpdaterModel] Debug mode detected, skipping update");
                    newVersionFolder = AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    Log.Instance.Debug("[ApplicationUpdaterModel] Applying releases...");
                    newVersionFolder = await mgr.ApplyReleases(updateInfo);
                }

                var lastAppliedRelease = updateInfo.ReleasesToApply.Last();

                Log.Instance.Debug(
                    $"[ApplicationUpdaterModel] Update completed to v{lastAppliedRelease.Version}, result: {newVersionFolder}");

                if (string.IsNullOrWhiteSpace(newVersionFolder))
                {
                    throw new ApplicationException("Expected non-empty new version folder path");
                }

                MostRecentVersionAppFolder = new DirectoryInfo(newVersionFolder);
                MostRecentVersion = lastAppliedRelease.Version;
                return true;
            }
        }

        public async Task RestartApplication()
        {
            var updatedExecutable = new FileInfo(Path.Combine(mostRecentVersionAppFolder.FullName, ApplicationName));
            Log.Instance.Debug(
                $"[ApplicationUpdaterModel] Restarting app, folder: {mostRecentVersionAppFolder}, appName: {ApplicationName}, exePath: {updatedExecutable}(exists: {updatedExecutable.Exists})...");

            if (!updatedExecutable.Exists)
            {
                throw new FileNotFoundException("Application executable was not found", updatedExecutable.FullName);
            }

            var squirrelUpdater = GetSquirrelUpdateExe();
            var squirrelArgs = $"--processStartAndWait {updatedExecutable.FullName}";

            Log.Instance.Debug($"[ApplicationUpdaterModel] Starting Squirrel updater @ '{squirrelUpdater}', args: {squirrelArgs} ...");
            var updaterProcess = Process.Start(squirrelUpdater, squirrelArgs);
            if (updaterProcess == null)
            {
                throw new FileNotFoundException($"Failed to start updater @ '{squirrelUpdater}'");
            }

            Log.Instance.Debug($"[ApplicationUpdaterModel] Process spawned, PID: {updaterProcess.Id}");
            await Task.Delay(2000);

            Log.Instance.Debug($"[ApplicationUpdaterModel] Terminating application...");
            Application.Current.Shutdown(0);
        }

        private void UpdateProgress(int progressPercent)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.UpdateProgress] Update is in progress: {progressPercent}%");
        }

        private void CheckUpdateProgress(int progressPercent)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.CheckUpdateProgress] Check update is in progress: {progressPercent}%");
        }

        private void OnAppUninstall(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.OnAppUninstall] Uninstalling v{appVersion}...");
        }

        private void OnAppUpdate(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.OnAppUpdate] Updateing v{appVersion}...");
        }

        private void OnInitialInstall(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.OnInitialInstall] App v{appVersion} installed");
        }

        private void OnFirstRun()
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.OnFirstRun] App started for the first time");
        }

        private static string GetSquirrelUpdateExe()
        {
            const string updaterExecutableName = "update.exe";

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null &&
                Path.GetFileName(entryAssembly.Location).Equals(updaterExecutableName, StringComparison.OrdinalIgnoreCase) &&
                entryAssembly.Location.IndexOf("app-", StringComparison.OrdinalIgnoreCase) == -1 &&
                entryAssembly.Location.IndexOf("SquirrelTemp", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return Path.GetFullPath(entryAssembly.Location);
            }

            var squirrelAssembly = typeof(UpdateManager).Assembly;
            var executingAssembly = Path.GetDirectoryName(squirrelAssembly.Location);
            if (executingAssembly == null)
            {
                throw new ApplicationException($"Failed to get executing of assembly {squirrelAssembly}");
            }

            var fileInfo = new FileInfo(Path.Combine(executingAssembly, "..", updaterExecutableName));
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"{updaterExecutableName} not found(path: {fileInfo.FullName}), not a Squirrel-installed app?",
                                                fileInfo.FullName);
            }

            return fileInfo.FullName;
        }
    }
}