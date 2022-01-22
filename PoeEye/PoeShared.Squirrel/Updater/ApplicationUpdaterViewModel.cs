using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Core;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Squirrel;
using Unity;

namespace PoeShared.Squirrel.Updater;

internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject, IApplicationUpdaterViewModel
{
    private static readonly Binder<ApplicationUpdaterViewModel> Binder = new();
    private static readonly IFluentLog Log = typeof(ApplicationUpdaterViewModel).PrepareLogger();
    private readonly SourceCache<IReleaseEntry, string> availableReleasesSource = new(x => x.EntryAsString);
    private readonly IScheduler uiScheduler;
    private readonly IFactory<IUpdaterWindowDisplayer> updateWindowDisplayer;
    private readonly IApplicationUpdaterModel updaterModel;

    static ApplicationUpdaterViewModel()
    {
        Binder.Bind(x => x.updaterModel.IsBusy || x.RestartCommand.IsBusy || x.ApplyUpdateCommand.IsBusy || x.CheckForUpdatesCommand.IsBusy).To((x, v) => x.IsBusy = v, x => x.uiScheduler);
    }

    public ApplicationUpdaterViewModel(
        IFactory<IUpdaterWindowDisplayer> updateWindowDisplayer,
        IApplicationUpdaterModel updaterModel,
        IConfigProvider<UpdateSettingsConfig> configProvider,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
        [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
    {
        Guard.ArgumentNotNull(updateWindowDisplayer, nameof(updateWindowDisplayer));
        Guard.ArgumentNotNull(configProvider, nameof(configProvider));
        Guard.ArgumentNotNull(updaterModel, nameof(updaterModel));
        Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
        Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

        this.updateWindowDisplayer = updateWindowDisplayer;
        this.updaterModel = updaterModel;
        this.uiScheduler = uiScheduler;

        CheckForUpdatesCommand = CommandWrapper
            .Create(CheckForUpdatesCommandExecuted, updaterModel.WhenAnyValue(x => x.UpdateSource).Select(x => x.IsValid));

        CheckForUpdatesCommand
            .ThrownExceptions
            .SubscribeToErrors(ex => SetError($"Update error: {ex.Message}"))
            .AddTo(Anchors);

        configProvider.ListenTo(x => x.AutoUpdateTimeout)
            .ObserveOn(uiScheduler)
            .SubscribeSafe(x => CheckForUpdates = x > TimeSpan.Zero, Log.HandleUiException)
            .AddTo(Anchors);

        availableReleasesSource
            .Connect()
            .Filter(x => x.IsDelta == false)
            .Sort(new SortExpressionComparer<IReleaseEntry>().ThenByDescending(x => x.Version))
            .Top(10)
            .Bind(out var availableReleases)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        AvailableReleases = availableReleases;

        this.ObservableForProperty(x => x.CheckForUpdates, skipInitial: true).ToUnit()
            .ObserveOn(uiScheduler)
            .SubscribeSafe(() =>
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

            
        this.RaiseWhenSourceValue(x => x.LatestVersion, this, x => x.LatestUpdate, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.UpdateInfo, this, x => x.LatestUpdate, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.IsDeltaUpdate, this, x => x.LatestUpdate, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.TotalUpdateSize, this, x => x.LatestUpdate, uiScheduler).AddTo(Anchors);
            
        this.RaiseWhenSourceValue(x => x.LatestAppliedVersion, updaterModel, x => x.LatestAppliedVersion, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.IgnoreDeltaUpdates, updaterModel, x => x.IgnoreDeltaUpdates, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.ProgressPercent, updaterModel, x => x.ProgressPercent, uiScheduler).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.UpdateSource, updaterModel, x => x.UpdateSource, uiScheduler).AddTo(Anchors);

        RestartCommand = CommandWrapper
            .Create(RestartCommandExecuted);

        RestartCommand
            .ThrownExceptions
            .SubscribeSafe(ex => SetError($"Restart error: {ex.Message}"), Log.HandleUiException)
            .AddTo(Anchors);
            
        configProvider
            .ListenTo(x => x.IgnoreDeltaUpdates)
            .SubscribeSafe(x => updaterModel.IgnoreDeltaUpdates = x, Log.HandleUiException)
            .AddTo(Anchors);

        Observable.Merge(
                configProvider
                    .ListenTo(x => x.AutoUpdateTimeout)
                    .WithPrevious((prev, curr) => new {prev, curr})
                    .Do(timeout => Log.Debug(() => $"AutoUpdate timeout changed: {timeout.prev} => {timeout.curr}"))
                    .Select(
                        timeout => timeout.curr <= TimeSpan.Zero
                            ? Observable.Never<long>()
                            : Observable.Timer(DateTimeOffset.MinValue, timeout.curr, bgScheduler))
                    .Switch()
                    .Select(_ => $"auto-update timer tick(timeout: {configProvider.ActualConfig.AutoUpdateTimeout})"),
                updaterModel
                    .ObservableForProperty(x => x.UpdateSource, skipInitial: true)
                    .Do(source => Log.Debug(() => $"Update source changed: {source}"))
                    .Select(x => $"update source change"))
            .ObserveOn(uiScheduler)
            .Do(reason => Log.Debug(() => $"Checking for updates, reason: {reason}"))
            .Where(x => CheckForUpdatesCommand.CanExecute(null))
            .SubscribeSafe(() => CheckForUpdatesCommand.Execute(null), Log.HandleException)
            .AddTo(Anchors);

        ShowUpdaterCommand = CommandWrapper.Create<object>(ShowUpdaterCommandExecuted);
        ApplyUpdateCommand = CommandWrapper.Create(
            ApplyUpdateCommandExecuted,
            this.WhenAnyValue(x => x.LatestUpdate).ObserveOn(uiScheduler).Select(x => x != null));

        OpenUri = CommandWrapper.Create<string>(OpenUriCommandExecuted);
        Binder.Attach(this).AddTo(Anchors);
    }

    public string UpdateInfo => LatestUpdate?.ReleasesToApply.EmptyIfNull().Select(x => $"{x.Version} (delta: {x.IsDelta})").JoinStrings(" => ");

    public bool IsDeltaUpdate => LatestUpdate?.ReleasesToApply?.EmptyIfNull().Any(x => x.IsDelta) ?? false;

    public long TotalUpdateSize => LatestUpdate?.ReleasesToApply?.EmptyIfNull().Sum(x => x.Filesize) ?? 0;

    public bool IgnoreDeltaUpdates => updaterModel.IgnoreDeltaUpdates;

    public CommandWrapper CheckForUpdatesCommand { get; }

    public CommandWrapper RestartCommand { get; }

    public CommandWrapper ApplyUpdateCommand { get; }
        
    public CommandWrapper ShowUpdaterCommand { get; }

    public bool IsInErrorStatus { get; private set; }

    public string StatusText { get; private set; }

    public bool IsOpen { get; set; }

    public ReadOnlyObservableCollection<IReleaseEntry> AvailableReleases { get; }

    public bool CheckForUpdates { get; set; }

    public IPoeUpdateInfo LatestUpdate { get; private set; }

    public Version LatestAppliedVersion => updaterModel.LatestAppliedVersion;

    public Version LatestVersion => LatestUpdate?.FutureReleaseEntry?.Version?.Version;

    public UpdateSourceInfo UpdateSource => updaterModel.UpdateSource;

    public int ProgressPercent => updaterModel.ProgressPercent;

    public bool IsBusy { get; private set; }

    public CommandWrapper OpenUri { get; }

    public FileInfo GetLatestExecutable()
    {
        return updaterModel.GetLatestExecutable();
    }

    public async Task PrepareForceUpdate(IReleaseEntry targetRelease)
    {
        Log.Debug(() => $"Force update preparation requested, target: {new { targetRelease.Version, targetRelease.Filename, targetRelease.Filesize }}");
        LatestUpdate = await updaterModel.PrepareForceUpdate(targetRelease);
        updaterModel.Reset();
        SetStatus($"Ready to update to v{LatestVersion}");
    }

    private void ShowUpdaterCommandExecuted(object arg)
    {
        Log.Debug(() => $"Show updater command executed, arg: {arg}");
        var owner = arg switch
        {
            Window wnd => wnd,
            _ => default,
        };
        var displayer = updateWindowDisplayer.Create();
        displayer.ShowDialog(new UpdaterWindowArgs()
        {
            AllowTermination = false,
            Owner = owner
        });
    }

    private async Task OpenUriCommandExecuted(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return;
        }

        Log.Debug(() => $"Preparing to open uri {uri}");
        await Task.Run(() =>
        {
            Log.Debug(() => $"Starting new process for uri: {uri}");
            var result = new Process {StartInfo = {FileName = uri, UseShellExecute = true}};
            if (!result.Start())
            {
                Log.Warn($"Failed to start process");
            }
            else
            {
                Log.Debug(() => $"Started new process for uri {uri}: { new { result.Id, result.ProcessName } }");
            }
        });
    }

    private async Task CheckForUpdatesCommandExecuted()
    {
        Log.Debug(() => $"Update check requested, source: {updaterModel.UpdateSource}");
        if (CheckForUpdatesCommand.IsBusy || ApplyUpdateCommand.IsBusy)
        {
            Log.Debug("Update is already in progress");
            IsOpen = true;
            return;
        }

        SetStatus("Checking for updates...");
        var sw = Stopwatch.StartNew();
        LatestUpdate = default;
        updaterModel.Reset();

        try
        {
            await updaterModel.CheckForUpdates();

            var timeToWait = sw.Elapsed - UiConstants.ArtificialShortDelay;
            if (timeToWait > TimeSpan.Zero)
            {
                // delaying update so the user could see the progress ring
                await Task.Delay(UiConstants.ArtificialShortDelay);
            }
                
            var newVersion = updaterModel.LatestUpdate;
            if (newVersion == null)
            {
                throw new ApplicationException($"Something went wrong - failed to get latest update info");
            }
            availableReleasesSource.EditDiff(newVersion.RemoteReleases);
            LatestUpdate = newVersion;
            if (newVersion.ReleasesToApply.Any())
            {
                IsOpen = true;
                SetStatus($"New version v{LatestVersion} is available");
            }
            else
            {
                SetStatus("No updates found");
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
        Log.Debug(() => $"Applying update {LatestVersion} (updated version: {LatestAppliedVersion})");
        if (CheckForUpdatesCommand.IsBusy || ApplyUpdateCommand.IsBusy)
        {
            Log.Debug("Already in progress");
            IsOpen = true;
            return;
        }

        SetStatus($"Preparing update v{LatestVersion}...");

        if (LatestUpdate == null)
        {
            throw new ApplicationException("Latest update must be specified");
        }

        await Task.Delay(UiConstants.ArtificialLongDelay);

        try
        {
            await updaterModel.ApplyRelease(LatestUpdate);
            IsOpen = true;
            SetStatus($"Successfully updated to v{LatestVersion}");
            LatestUpdate = default;
        }
        catch (Exception ex)
        {
            Log.HandleException(ex);
            IsOpen = true;
            SetError($"Failed to apply update to v{LatestVersion}, UpdateSource: {UpdateSource} - {ex.Message}");
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