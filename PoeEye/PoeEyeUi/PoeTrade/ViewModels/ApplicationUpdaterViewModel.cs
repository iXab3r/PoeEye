namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared;

    using ReactiveUI;

    using Squirrel;
    using System;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Input;

    using DumpToText;

    internal sealed class ApplicationUpdaterViewModel : ReactiveObject
    {
        private const string PoeEyeUri = @"http://coderush.net/files/PoeEye";
        private readonly ReactiveCommand<object> checkForUpdatesCommand;

        public ApplicationUpdaterViewModel()
        {
            checkForUpdatesCommand = ReactiveCommand.Create();
            checkForUpdatesCommand.Subscribe(CheckForUpdatesCommandExecuted);

            Observable
                .Timer(DateTimeOffset.Now, UpdatePeriod)
                .Subscribe(_ => CheckForUpdatesCommandExecuted(null));

            SquirrelAwareApp.HandleEvents(
                      onInitialInstall: OnInitialInstall,
                      onAppUpdate: OnAppUpdate,
                      onAppUninstall: OnAppUninstall,
                      onFirstRun: OnFirstRun);
        }

        public ICommand CheckForUpdatesCommand => checkForUpdatesCommand;

        private async void CheckForUpdatesCommandExecuted(object arg)
        {

#if DEBUG
            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Debug mode detected...");
            return;
#endif
            try
            {
                var appName = typeof(PoeEye.Prism.LiveRegistrations).Assembly.GetName().Name;
                using (var mgr = new UpdateManager(PoeEyeUri, appName))
                {
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Checking for updates...");

                    var updateInfo = await mgr.CheckForUpdate();

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] UpdateInfo:\r\n{updateInfo?.DumpToTextValue()}");
                    if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
                    {
                        return;
                    }
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Downloading releases...");
                    await mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress);
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Applying releases...");
                    await mgr.ApplyReleases(updateInfo);
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update completed");
                    MessageBox.Show("Application updated, new version will take place on next application startup", "Update completed",MessageBoxButton.OK,MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update failed", ex);
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

        public TimeSpan UpdatePeriod => TimeSpan.FromMinutes(5);
    }
}