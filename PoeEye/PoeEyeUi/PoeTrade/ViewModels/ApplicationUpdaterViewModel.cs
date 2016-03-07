namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    using Squirrel;

    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject
    {
        private const string PoeEyeUri = @"http://coderush.net/files/PoeEye";
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);
        private readonly ReactiveCommand<object> checkForUpdatesCommand;

        private readonly Version defaultVersion;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ReactiveCommand<object> restartCommand;
        private readonly IScheduler uiScheduler;

        private bool isBusy;

        private bool isOpen;

        private Version mostRecentVersion;

        private string mostRecentVersionAppFolder;

        public ApplicationUpdaterViewModel(
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => dialogCoordinator);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            this.dialogCoordinator = dialogCoordinator;
            this.uiScheduler = uiScheduler;

            checkForUpdatesCommand = ReactiveCommand.Create();
            checkForUpdatesCommand
                .Where(x => !IsBusy)
                .ObserveOn(bgScheduler)
                .Subscribe(_ => CheckForUpdatesCommandExecuted(), Log.HandleUiException)
                .AddTo(Anchors);

            SquirrelAwareApp.HandleEvents(  
                OnInitialInstall,
                OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);

            defaultVersion = Assembly.GetExecutingAssembly().GetName().Version;

            MostRecentVersionAppFolder = AppDomain.CurrentDomain.BaseDirectory;
            MostRecentVersion = defaultVersion;

            restartCommand = ReactiveCommand.Create();
            restartCommand.Subscribe(RestartCommandExecuted).AddTo(Anchors);
        }

        public ICommand CheckForUpdatesCommand => checkForUpdatesCommand;

        public ICommand RestartCommand => restartCommand;

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public string MostRecentVersionAppFolder
        {
            get { return mostRecentVersionAppFolder; }
            set { this.RaiseAndSetIfChanged(ref mostRecentVersionAppFolder, value); }
        }

        public Version MostRecentVersion
        {
            get { return mostRecentVersion; }
            set { this.RaiseAndSetIfChanged(ref mostRecentVersion, value); }
        }

        private void CheckForUpdatesCommandExecuted()
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update check requested");
            IsBusy = true;

            // delaying update so the user could see the progressring
            Thread.Sleep(ArtificialDelay);

            try
            {
                var appName = typeof (ApplicationUpdaterViewModel).Assembly.GetName().Name;
                using (var mgr = new UpdateManager(PoeEyeUri, appName))
                {
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Checking for updates...");

                    var updateInfo = mgr.CheckForUpdate().Result;

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] UpdateInfo:\r\n{updateInfo?.DumpToText()}");
                    if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
                    {
                        return;
                    }

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Downloading releases...");
                    mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress).Wait();

#if DEBUG
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Debug mode detected, skipping update");
                    var newVersionFolder = AppDomain.CurrentDomain.BaseDirectory;
#else
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Applying releases...");
                    var newVersionFolder = mgr.ApplyReleases(updateInfo).Result;
#endif

                    var lastAppliedRelease = updateInfo.ReleasesToApply.Last();

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update completed to v{lastAppliedRelease.Version}, result: {newVersionFolder}");

                    if (string.IsNullOrWhiteSpace(newVersionFolder))
                    {
                        throw new ApplicationException("Expected non-empty new version folder path");
                    }

                    MostRecentVersionAppFolder = newVersionFolder;
                    MostRecentVersion = lastAppliedRelease.Version;
                    IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                uiScheduler.Schedule(ShowUpdateFailedMessageAndTerminate);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateProgress(int progressPercent)
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel.UpdateProgress] Update in progress: {progressPercent}%");
        }

        private void OnAppUninstall(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel.OnAppUninstall] Uninstalling v{appVersion}...");
        }

        private void OnAppUpdate(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel.OnAppUpdate] Updateing v{appVersion}...");
        }

        private void OnInitialInstall(Version appVersion)
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel.OnInitialInstall] App v{appVersion} installed");
        }

        private void OnFirstRun()
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel.OnFirstRun] App started for the first time");
        }

        private void ShowUpdateFailedMessageAndTerminate()
        {
            var mainWindow = dialogCoordinator.MainWindow;
            if (mainWindow == null)
            {
                throw new ApplicationException("Main window is not set");
            }

            dialogCoordinator
                .ShowMessageAsync(
                    mainWindow,
                    "System error",
                    "Failed to connect to update server");
        }

        private void RestartCommandExecuted()
        {
            var applicationName = Process.GetCurrentProcess().ProcessName + ".exe";

            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Restarting app, folder: {mostRecentVersionAppFolder}, appName: {applicationName}...");
            var updatedExePath = new FileInfo(Path.Combine(mostRecentVersionAppFolder, applicationName));

            //FIXME Race condition, it's possible that application will be loaded BEFORE this instance will be unloaded => mutex conflict
            Process.Start(updatedExePath.FullName);
            Application.Current.Shutdown(0);
        }
    }
}