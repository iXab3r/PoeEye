using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using ByteSizeLib;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
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
    private readonly ISharedResourceLatch isBusyLatch;

    static ApplicationUpdaterViewModel()
    {
        Binder.Bind(x => x.isBusyLatch.IsBusy || x.updaterModel.IsBusy || x.RestartCommand.IsBusy || x.ApplyUpdateCommand.IsBusy || x.CheckForUpdatesCommand.IsBusy).To((x, v) => x.IsBusy = v, x => x.uiScheduler);
        Binder.Bind(x => x.LatestUpdate != null).OnScheduler(x => x.uiScheduler).To(x => x.CanUpdateToLatest);
        Binder.Bind(x => x.LatestUpdate != null && x.LatestUpdate.ReleasesToApply.EmptyIfNull().Any()).OnScheduler(x => x.uiScheduler).To(x => x.HasUpdatesToInstall);
    }

    public ApplicationUpdaterViewModel(
        IFactory<IUpdaterWindowDisplayer> updateWindowDisplayer,
        IApplicationUpdaterModel updaterModel,
        IApplicationAccessor applicationAccessor,
        IConfigProvider<UpdateSettingsConfig> configProvider,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
        [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiIdleScheduler,
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
        this.isBusyLatch = new SharedResourceLatch().AddTo(Anchors);

        var checkForUpdatesCommandSink = new Subject<Unit>();
        CheckForUpdatesCommand = CommandWrapper
            .Create<object>(async x => { checkForUpdatesCommandSink.OnNext(Unit.Default); }, updaterModel.WhenAnyValue(x => x.UpdateSource).Select(x => x.IsValid));

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
        this.RaiseWhenSourceValue(x => x.LauncherExecutable, updaterModel, x => x.LauncherExecutable, uiScheduler).AddTo(Anchors);
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

        configProvider
            .ListenTo(x => x.AutomaticallyDownloadUpdates)
            .SubscribeSafe(x => AutomaticallyDownloadUpdates = x, Log.HandleUiException)
            .AddTo(Anchors);

        Observable.Merge(
                configProvider
                    .ListenTo(x => x.AutoUpdateTimeout)
                    .EnableIf(applicationAccessor.WhenAnyValue(x => x.IsLoaded))
                    .WithPrevious((prev, curr) => new {prev, curr})
                    .Do(timeout => Log.Debug($"AutoUpdate timeout changed: {timeout.prev} => {timeout.curr}"))
                    .Select(
                        timeout => timeout.curr <= TimeSpan.Zero
                            ? Observable.Never<long>()
                            : Observable.Interval(timeout.curr))
                    .Switch()
                    .Select(_ => new {open = false, silent = true, reason = $"auto-update timer tick(timeout: {configProvider.ActualConfig.AutoUpdateTimeout})"})
                    .Skip(1),
                updaterModel
                    .ObservableForProperty(x => x.UpdateSource, skipInitial: true)
                    .Do(source => Log.Debug($"Update source changed: {source}"))
                    .Select(x => new {open = false, silent = false, reason = $"update source change"}),
                applicationAccessor
                    .WhenAnyValue(x => x.IsLoaded)
                    .Where(x => x)
                    .Where(x => configProvider.ActualConfig.AutoUpdateTimeout > TimeSpan.Zero)
                    .Delay(TimeSpan.FromSeconds(60))
                    .Select(x => new {open = false, silent = false, reason = $"initial tick after app loads"}),
                this.WhenAnyValue(x => x.AutomaticallyDownloadUpdates)
                    .Where(x => x)
                    .Skip(1)
                    .Select(x => new {open = true, silent = false, reason = $"settings changed, {nameof(AutomaticallyDownloadUpdates)} enabled"}),
                checkForUpdatesCommandSink.Select(_ => new {open = true, silent = false, reason = $"user requested update"}))
            .ObserveOn(uiIdleScheduler)
            .Do(x => Log.Debug($"Checking for updates: {x}"))
            .Where(x => !IsBusy)
            .SubscribeAsync(async x => { await CheckForUpdate(open: x.open, silent: x.silent); })
            .AddTo(Anchors);

        ShowUpdaterCommand = CommandWrapper.Create<object>(ShowUpdaterCommandExecuted);
        ApplyUpdateCommand = CommandWrapper.Create<object>(
            x => ApplyUpdateCommandExecuted(applyRelease: x is bool b ? b : true),
            this.WhenAnyValue(x => x.CanUpdateToLatest).ObserveOn(uiScheduler));

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

    public bool CanUpdateToLatest { get; [UsedImplicitly] private set; }

    public bool HasUpdatesToInstall { get; [UsedImplicitly] private set; }

    public bool AutomaticallyDownloadUpdates { get; private set; }

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

    public FileInfo LauncherExecutable => updaterModel.LauncherExecutable;

    public async Task PrepareForceUpdate(IReleaseEntry targetRelease)
    {
        Log.Debug($"Force update preparation requested, target: {new {targetRelease.Version, targetRelease.Filename, targetRelease.Filesize}}");
        LatestUpdate = await updaterModel.PrepareForceUpdate(targetRelease);
        updaterModel.Reset();
        SetStatus($"Ready to update to v{LatestVersion}");
    }

    private void ShowUpdaterCommandExecuted(object arg)
    {
        Log.Debug($"Show updater command executed, arg: {arg}");
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

        Log.Debug($"Preparing to open uri {uri}");
        await Task.Run(() =>
        {
            Log.Debug($"Starting new process for uri: {uri}");
            var result = new Process {StartInfo = {FileName = uri, UseShellExecute = true}};
            if (!result.Start())
            {
                Log.Warn($"Failed to start process");
            }
            else
            {
                Log.Debug($"Started new process for uri {uri}: {new {result.Id, result.ProcessName}}");
            }
        });
    }

    private async Task CheckForUpdate(bool open, bool silent)
    {
        if (IsBusy)
        {
            Log.Debug("Update is already in progress");
            return;
        }

        Log.Debug($"Update check requested, source: {updaterModel.UpdateSource}");
        SetStatus("Checking for updates...");

        using var isBusyRent = isBusyLatch.Rent();
        if (open)
        {
            IsOpen = true;
            await Task.Delay(UiConstants.ArtificialShortDelay);
        }

        try
        {
            var sw = Stopwatch.StartNew();
            updaterModel.Reset();

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
                throw new ApplicationException($"Failed to get latest update info");
            }

            availableReleasesSource.EditDiff(newVersion.RemoteReleases);
            LatestUpdate = newVersion;
            if (newVersion.ReleasesToApply.Any())
            {
                SetStatus($"New version v{LatestVersion} is available");
                if (AutomaticallyDownloadUpdates)
                {
                    await ApplyUpdateCommandExecuted(applyRelease: false);
                }
            }
            else
            {
                SetStatus($"No updates in {updaterModel.UpdateSource.Name} channel, you're using the latest version");
            }
        }
        catch (Exception ex)
        {
            Log.HandleException(ex);
            SetError($"Failed to check for updates, UpdateSource: {UpdateSource} - {ex.Message}");
            if (!silent)
            {
                IsOpen = true;
            }
        }
    }

    private async Task ApplyUpdateCommandExecuted(bool applyRelease)
    {
        Log.Debug($"Applying update {LatestVersion} (updated version: {LatestAppliedVersion})");
        if (ApplyUpdateCommand.IsBusy)
        {
            Log.Debug("Already in progress");
            return;
        }

        SetStatus($"Preparing to update to v{LatestVersion}...");

        var latestUpdate = LatestUpdate;
        if (latestUpdate == null)
        {
            throw new InvalidOperationException("Latest update must be specified");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            SetStatus($"Verifying installation files v{LatestVersion} ({ByteSize.FromBytes(latestUpdate.ReleasesToApply.EmptyIfNull().Sum(x => x.Filesize)).MegaBytes:F0} MB)...");
            var isVerified = await updaterModel.VerifyRelease(latestUpdate);
            SetStatus($"Verified v{LatestVersion}");

            if (!isVerified)
            {
                SetStatus($"Downloading v{LatestVersion} ({ByteSize.FromBytes(latestUpdate.ReleasesToApply.EmptyIfNull().Sum(x => x.Filesize)).MegaBytes:F0} MB)...");
                await updaterModel.DownloadRelease(latestUpdate);
                SetStatus($"Downloaded v{LatestVersion}");
            }

            if (!applyRelease)
            {
                return;
            }

            SetStatus($"Installing v{LatestVersion}...");
            await updaterModel.ApplyRelease(latestUpdate);
            IsOpen = true;
            SetStatus($"Successfully updated to v{LatestVersion}, restarting...");
            LatestUpdate = default;
            await RestartCommandExecuted();
        }
        catch (Exception ex)
        {
            Log.HandleException(ex);
            IsOpen = true;
            SetError($"Failed to apply update to v{LatestVersion}, UpdateSource: {UpdateSource} - {ex.Message}");
        }
        finally
        {
            var timeToSleep = UiConstants.ArtificialShortDelay - sw.Elapsed;
            if (timeToSleep > TimeSpan.Zero)
            {
                await Task.Delay(timeToSleep);
            }
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

            await updaterModel.RestartApplicationViaLauncher();
        }
        catch (Exception ex)
        {
            IsOpen = true;

            Log.HandleUiException(ex);
            SetError($"Failed to restart application - {ex.Message}");
        }
    }
}