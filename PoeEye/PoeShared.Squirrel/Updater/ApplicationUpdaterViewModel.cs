using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using ReactiveUI;
using Unity;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject, IApplicationUpdaterViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationUpdaterViewModel));

        private readonly IApplicationUpdaterModel updaterModel;

        private bool isOpen;
        private string statusText;
        private bool isInErrorStatus;
        private bool checkForUpdates;

        public ApplicationUpdaterViewModel(
            [NotNull] IApplicationUpdaterModel updaterModel,
            [NotNull] IConfigProvider<UpdateSettingsConfig> configProvider,
            [NotNull] IUpdateSourceProvider updateSourceProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(updaterModel, nameof(updaterModel));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.updaterModel = updaterModel;

            CheckForUpdatesCommand = CommandWrapper
                .Create(CheckForUpdatesCommandExecuted, updaterModel.WhenAnyValue(x => x.UpdateSource).Select(x => x.IsValid));

            CheckForUpdatesCommand
                .ThrownExceptions
                .Subscribe(ex => SetError($"Update error: {ex.Message}"))
                .AddTo(Anchors);

            configProvider.ListenTo(x => x.AutoUpdateTimeout)
                .ObserveOn(uiScheduler)
                .Subscribe(x => CheckForUpdates = x > TimeSpan.Zero)
                .AddTo(Anchors);

            this.ObservableForProperty(x => x.CheckForUpdates, skipInitial: true).ToUnit()
                .ObserveOn(uiScheduler)
                .Subscribe(() =>
                {
                    var updateConfig = configProvider.ActualConfig.CloneJson();
                    if (CheckForUpdates && updateConfig.AutoUpdateTimeout <= TimeSpan.Zero)
                    {
                        updateConfig.AutoUpdateTimeout = UpdateSettingsConfig.DefaultAutoUpdateTimeout;
                    }
                    else if (!CheckForUpdates)
                    {
                        updateConfig.AutoUpdateTimeout = TimeSpan.Zero;
                    }
                    configProvider.Save(updateConfig);
                }, Log.HandleUiException)
                .AddTo(Anchors);

            this.RaiseWhenSourceValue(x => x.UpdatedVersion, updaterModel, x => x.UpdatedVersion, uiScheduler).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.LatestVersion, updaterModel, x => x.LatestVersion, uiScheduler).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.ProgressPercent, updaterModel, x => x.ProgressPercent, uiScheduler).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.IsBusy, updaterModel, x => x.IsBusy, uiScheduler).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.UpdateSource, updaterModel, x => x.UpdateSource, uiScheduler).AddTo(Anchors);

            RestartCommand = CommandWrapper
                .Create(RestartCommandExecuted);

            RestartCommand
                .ThrownExceptions
                .Subscribe(ex => SetError($"Restart error: {ex.Message}"))
                .AddTo(Anchors);

            updateSourceProvider
                .WhenAnyValue(x => x.UpdateSource)
                .Subscribe(x => updaterModel.UpdateSource = x)
                .AddTo(Anchors);

            Observable.Merge(
                    configProvider
                        .ListenTo(x => x.AutoUpdateTimeout)
                        .WithPrevious((prev, curr) => new {prev, curr})
                        .Do(timeout => Log.Debug($"AutoUpdate timeout changed: {timeout.prev} => {timeout.curr}"))
                        .Select(
                            timeout => timeout.curr <= TimeSpan.Zero
                                ? Observable.Never<long>()
                                : Observable.Timer(DateTimeOffset.MinValue, timeout.curr, bgScheduler))
                        .Switch()
                        .Select(_ => $"auto-update timer tick(timeout: {configProvider.ActualConfig.AutoUpdateTimeout})"),
                    updaterModel
                        .ObservableForProperty(x => x.UpdateSource, skipInitial: true)
                        .Do(source => Log.Debug($"Update source changed: {source}"))
                        .Select(x => $"update source change"))
                .ObserveOn(uiScheduler)
                .Do(reason => Log.Debug($"Checking for updates, reason: {reason}"))
                .Where(x => CheckForUpdatesCommand.CanExecute(null))
                .Subscribe(() => CheckForUpdatesCommand.Execute(null), Log.HandleException)
                .AddTo(Anchors);

            ApplyUpdate = CommandWrapper.Create(
                ApplyUpdateCommandExecuted,
                this.updaterModel.WhenAnyValue(x => x.LatestVersion).ObserveOn(uiScheduler).Select(x => x != null));

            OpenUri = CommandWrapper.Create<string>(OpenUriCommandExecuted);
        }

        private async Task OpenUriCommandExecuted(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return;
            }

            await Task.Run(() => Process.Start(uri));
        }

        public CommandWrapper CheckForUpdatesCommand { get; }

        public CommandWrapper RestartCommand { get; }

        public CommandWrapper ApplyUpdate { get; }

        public bool IsInErrorStatus
        {
            get => isInErrorStatus;
            private set => RaiseAndSetIfChanged(ref isInErrorStatus, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => RaiseAndSetIfChanged(ref statusText, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }

        public bool CheckForUpdates
        {
            get => checkForUpdates;
            set => this.RaiseAndSetIfChanged(ref checkForUpdates, value);
        }

        [CanBeNull] public Version UpdatedVersion => updaterModel.UpdatedVersion;

        [CanBeNull] public Version LatestVersion => updaterModel.LatestVersion?.FutureReleaseEntry?.Version?.Version;
        
        public UpdateSourceInfo UpdateSource => updaterModel.UpdateSource;
        
        public int ProgressPercent => updaterModel.ProgressPercent;
        
        public bool IsBusy => updaterModel.IsBusy;

        public CommandWrapper OpenUri { get; }

        public FileInfo GetLatestExecutable()
        {
            return updaterModel.GetLatestExecutable();
        }

        private async Task CheckForUpdatesCommandExecuted()
        {
            Log.Debug($"Update check requested, source: {updaterModel.UpdateSource}");
            if (CheckForUpdatesCommand.IsBusy || ApplyUpdate.IsBusy)
            {
                Log.Debug("Update is already in progress");
                IsOpen = true;
                return;
            }

            SetStatus("Checking for updates...");
            updaterModel.Reset();

            // delaying update so the user could see the progress ring
            await Task.Delay(UiConstants.ArtificialLongDelay);

            try
            {
                var newVersion = await Task.Run(updaterModel.CheckForUpdates);

                if (newVersion != null)
                {
                    IsOpen = true;
                    SetStatus($"New version available is available");
                }
                else
                {
                    SetStatus("Latest version is already installed");
                }
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                IsOpen = true;
                SetError($"Failed to check for updates, UpdateSource: {UpdateSource} - {ex.Message}");
            }
        }

        private async Task ApplyUpdateCommandExecuted()
        {
            Log.Debug($"Applying latest update {LatestVersion} (updated: {UpdatedVersion})");
            if (CheckForUpdatesCommand.IsBusy || ApplyUpdate.IsBusy)
            {
                Log.Debug("Already in progress");
                IsOpen = true;
                return;
            }

            SetStatus($"Downloading and Applying update {LatestVersion}...");

            if (updaterModel.LatestVersion == null)
            {
                throw new ApplicationException("Latest version must be specified");
            }

            await Task.Delay(UiConstants.ArtificialLongDelay);

            try
            {
                await updaterModel.ApplyRelease(updaterModel.LatestVersion);
                IsOpen = true;
                SetStatus($"Successfully updated to the version {LatestVersion}");
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                IsOpen = true;
                SetError($"Failed to apply update {LatestVersion}, UpdateSource: {UpdateSource} - {ex.Message}");
            }
        }

        private void SetStatus(string text)
        {
            IsInErrorStatus = false;
            StatusText = text;
        }

        private void SetError(string text)
        {
            IsInErrorStatus = true;
            StatusText = text;
            updaterModel.Reset();
        }
        
        private async Task RestartCommandExecuted()
        {
            Log.Debug("Restart application requested");

            try
            {
                IsOpen = true;
                SetStatus("Restarting application...");

                await updaterModel.RestartApplication();
            }
            catch (Exception ex)
            {
                IsOpen = true;

                Log.HandleUiException(ex);
                SetError($"Failed to restart application - {ex.Message}");
            }
        }
    }
}