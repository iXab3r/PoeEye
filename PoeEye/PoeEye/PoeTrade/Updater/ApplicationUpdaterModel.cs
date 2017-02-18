using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Squirrel;

namespace PoeEye.PoeTrade.Updater
{
    internal sealed class ApplicationUpdaterModel : DisposableReactiveObject
    {
        private static readonly string PoeEyeUri = @"http://coderush.net/files/PoeEye/";
        private static readonly string ApplicationName = Process.GetCurrentProcess().ProcessName + ".exe";

        private bool isBusy;
        private bool isOpen;

        private Version mostRecentVersion;

        private DirectoryInfo mostRecentVersionAppFolder;

        public ApplicationUpdaterModel()
        {
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
            get { return mostRecentVersionAppFolder; }
            set { this.RaiseAndSetIfChanged(ref mostRecentVersionAppFolder, value); }
        }

        public Version MostRecentVersion
        {
            get { return mostRecentVersion; }
            set { this.RaiseAndSetIfChanged(ref mostRecentVersion, value); }
        }

        /// <summary>
        ///  Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        public async Task<bool> CheckForUpdates()
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel] Update check requested");

            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            var rootDirectory = default(string);

            if (App.Arguments.IsDebugMode)
            {
                rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            Log.Instance.Debug($"[ApplicationUpdaterModel] AppName: {appName}, root directory: {rootDirectory}");

            using (var mgr = new UpdateManager(PoeEyeUri, appName, rootDirectory))
            {
                Log.Instance.Debug($"[ApplicationUpdaterModel] Checking for updates...");

                var updateInfo = await mgr.CheckForUpdate();

                Log.Instance.Debug($"[ApplicationUpdaterModel] UpdateInfo:\r\n{updateInfo?.DumpToText()}");
                if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
                {
                    return false;
                }

                Log.Instance.Debug($"[ApplicationUpdaterModel] Downloading releases...");
                await mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress);

                string newVersionFolder;
                if (App.Arguments.IsDebugMode)
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

        private void UpdateProgress(int progressPercent)
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel.UpdateProgress] Update in progress: {progressPercent}%");
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

        public void RestartApplication()
        {
            Log.Instance.Debug($"[ApplicationUpdaterModel] Restarting app, folder: {mostRecentVersionAppFolder}, appName: {ApplicationName}...");
            var updatedExePath = new FileInfo(Path.Combine(mostRecentVersionAppFolder.FullName, ApplicationName));

            //FIXME Race condition, it's possible that the new application loads BEFORE this instance will be unloaded => mutex conflict
            if (!updatedExePath.Exists)
            {
                throw new FileNotFoundException("Application executable was not found", updatedExePath.FullName);
            }
            Process.Start(updatedExePath.FullName);
            Application.Current.Shutdown(0);
        }
    }
}