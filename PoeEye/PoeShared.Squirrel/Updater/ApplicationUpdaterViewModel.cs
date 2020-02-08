using System;
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
                        .Do(timeout => Log.Debug($"[ApplicationUpdaterViewModel] AutoUpdate timeout changed: {timeout.prev} => {timeout.curr}"))
                        .Select(
                            timeout => timeout.curr <= TimeSpan.Zero
                                ? Observable.Never<long>()
                                : Observable.Timer(DateTimeOffset.MinValue, timeout.curr, bgScheduler))
                        .Switch()
                        .ToUnit(),
                    updaterModel.WhenAnyProperty(x => x.UpdateSource).ToUnit())
                .ObserveOn(uiScheduler)
                .Where(x => CheckForUpdatesCommand.CanExecute(null))
                .Subscribe(() => CheckForUpdatesCommand.Execute(null), Log.HandleException)
                .AddTo(Anchors);

            ApplyUpdate = CommandWrapper.Create(
                ApplyUpdateCommandExecuted,
                this.updaterModel.WhenAnyValue(x => x.LatestVersion).ObserveOn(uiScheduler).Select(x => x != null));
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

        [CanBeNull] public Version UpdatedVersion => updaterModel.UpdatedVersion;

        [CanBeNull] public Version LatestVersion => updaterModel.LatestVersion?.FutureReleaseEntry?.Version?.Version;
        
        public UpdateSourceInfo UpdateSource => updaterModel.UpdateSource;
        
        public int ProgressPercent => updaterModel.ProgressPercent;
        
        public bool IsBusy => updaterModel.IsBusy;
        
        public (string exePath, string exeArgs) GetRestartApplicationArgs()
        {
            return updaterModel.GetRestartApplicationArgs();
        }

        private async Task CheckForUpdatesCommandExecuted()
        {
            Log.Debug($"[ApplicationUpdaterViewModel] Update check requested, source: {updaterModel.UpdateSource}");
            if (CheckForUpdatesCommand.IsBusy || ApplyUpdate.IsBusy)
            {
                Log.Debug("[ApplicationUpdaterViewModel] Already in progress");
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
            Log.Debug($"[ApplicationUpdaterViewModel] Applying latest update {LatestVersion} (updated: {UpdatedVersion})");
            if (CheckForUpdatesCommand.IsBusy || ApplyUpdate.IsBusy)
            {
                Log.Debug("[ApplicationUpdaterViewModel] Already in progress");
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
            Log.Debug("[ApplicationUpdaterViewModel] Restart application requested");

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