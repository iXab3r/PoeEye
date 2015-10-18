﻿namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared;

    using ReactiveUI;

    using Squirrel;
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;

    using DumpToText;

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using PoeShared.Utilities;

    using Utilities;

    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);
        private readonly IDialogCoordinator dialogCoordinator;
        private const string PoeEyeUri = @"http://coderush.net/files/PoeEye";
        private readonly ReactiveCommand<object> checkForUpdatesCommand;
        private readonly SynchronizationContext uiContext;

        public ApplicationUpdaterViewModel([NotNull] IDialogCoordinator dialogCoordinator)
        {
            Guard.ArgumentNotNull(() => dialogCoordinator);

            this.dialogCoordinator = dialogCoordinator;
            checkForUpdatesCommand = ReactiveCommand.Create();

            uiContext = SynchronizationContext.Current;

            checkForUpdatesCommand
                .Where(x => !IsBusy)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(CheckForUpdatesCommandExecuted, Log.HandleException)
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

            try
            {
                Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update check requested");
                IsBusy = true;

                // delaying update so the user could see the progressring
                Thread.Sleep(ArtificialDelay); 

#if DEBUG
                Log.Instance.Debug($"[ApplicationUpdaterViewModel] Debug mode detected, update will be skipped");
                return;
#endif
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
                        mgr.DownloadReleases(updateInfo.ReleasesToApply, UpdateProgress).RunSynchronously();
                        Log.Instance.Debug($"[ApplicationUpdaterViewModel] Applying releases...");
                        mgr.ApplyReleases(updateInfo).RunSynchronously();
                        Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update completed");

                        uiContext.Post(state => dialogCoordinator.ShowMessageAsync(context, "Update completed", "Application updated, new version will take place on next application startup"), null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update failed", ex);
                }
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
    }
}