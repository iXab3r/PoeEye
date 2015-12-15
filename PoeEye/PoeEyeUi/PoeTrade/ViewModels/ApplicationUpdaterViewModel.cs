namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared;

    using ReactiveUI;

    using Squirrel;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using MahApps.Metro.Controls.Dialogs;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeShared.DumpToText;
    using PoeShared.Utilities;

    using Prism;

    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IScheduler uiScheduler;
        private const string PoeEyeUri = @"http://coderush.net/files/PoeEye";
        private readonly ReactiveCommand<object> checkForUpdatesCommand;

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
                .Do(_ => IsBusy = true)
                .Do(CheckForUpdatesCommandExecuted, Log.HandleException)
                .Do(_ => IsBusy = false)
                .Subscribe()
                .AddTo(Anchors);

            SquirrelAwareApp.HandleEvents(
                      onInitialInstall: OnInitialInstall,
                      onAppUpdate: OnAppUpdate,
                      onAppUninstall: OnAppUninstall,
                      onFirstRun: OnFirstRun);
        }

        public ICommand CheckForUpdatesCommand => checkForUpdatesCommand;

        private bool isBusy;

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        private void CheckForUpdatesCommandExecuted(object context)
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update check requested");

            // delaying update so the user could see the progressring
            Thread.Sleep(ArtificialDelay);

            try
            {
                var appName = typeof(PoeEye.Prism.LiveRegistrations).Assembly.GetName().Name;
                using (var mgr = new UpdateManager(PoeEyeUri, appName))
                {
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Checking for updates...");

                    var updateInfo = mgr.CheckForUpdate().Result;

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] UpdateInfo:\r\n{updateInfo?.DumpToTextValue()}");
                    if (updateInfo == null || updateInfo.ReleasesToApply.Count == 0)
                    {
                        return;
                    }

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Downloading releases...");
                    mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress).Wait();
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Applying releases...");


                    var newVersionFolder = mgr.ApplyReleases(updateInfo).Result;
                    var lastAppliedRelease = updateInfo.ReleasesToApply.Last();

                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update completed to v{lastAppliedRelease.Version}, result: {newVersionFolder}");

                    if (string.IsNullOrWhiteSpace(newVersionFolder))
                    {
                        throw new ApplicationException("Expected non-empty new version folder path");
                    }

                    uiScheduler.Schedule(() => ShowUpdateCompletedMessageAndTerminate(context, newVersionFolder, lastAppliedRelease.Version));
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update failed", ex);

                uiScheduler.Schedule(() => ShowUpdateFailedMessageAndTerminate(context));
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

        private void ShowUpdateCompletedMessageAndTerminate(object context, string updatedVersionDir, Version updatedVersion)
        {
            var applicationName = Process.GetCurrentProcess().ProcessName + ".exe";
            var updatedExePath = new FileInfo(Path.Combine(updatedVersionDir, applicationName));

            if (updatedExePath.Exists)
            {
                dialogCoordinator
                   .ShowMessageAsync(
                        context, 
                        "Update completed", 
                        $"Application was successfully updated to v{updatedVersion}",
#if DEBUG
                        MessageDialogStyle.AffirmativeAndNegative,
#else
                        MessageDialogStyle.Affirmative, 
#endif
                        new MetroDialogSettings { AffirmativeButtonText = "Restart", NegativeButtonText = "Cancel" })
                   .ContinueWith(
                       (Task<MessageDialogResult> dialogResultTask) =>
                       {
                           var messageBoxResult = dialogResultTask.Result;
                           if (messageBoxResult == MessageDialogResult.Affirmative)
                           {
                               Log.Instance.Debug("[ApplicationUpdaterViewModel] App updated, restarting...");
                               Process.Start(updatedExePath.FullName);
                               Environment.Exit(0);
                           }
                           else
                           {
                               Log.Instance.Debug("[ApplicationUpdaterViewModel] App updated, restart was cancelled");
                           }
                       });
            }
            else
            {
                dialogCoordinator
                    .ShowMessageAsync(context, "Update was partially completed", "Application updated, new version will take place on next application startup", MessageDialogStyle.Affirmative, new MetroDialogSettings { AffirmativeButtonText = "Exit" })
                    .ContinueWith(
                        (x) =>
                        {
                            Log.Instance.Debug("[ApplicationUpdaterViewModel] App updated, terminating this instance");
                            Environment.Exit(0);
                        });
            }
        }

        private void ShowUpdateFailedMessageAndTerminate(object context)
        {
            dialogCoordinator
                .ShowMessageAsync(context, "System error", "Failed to connect to update server, application will be terminated due to security reasons", MessageDialogStyle.Affirmative, new MetroDialogSettings { AffirmativeButtonText = "Exit" })
                .ContinueWith(
                    (x) =>
                    {
                        Log.Instance.Debug("[ApplicationUpdaterViewModel] App failed to update, terminating this instance");
                        Environment.Exit(-1);
                    });
        }
    }
}