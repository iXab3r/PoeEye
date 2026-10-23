using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using ByteSizeLib;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared.Dialogs.Services;
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
    private readonly IMessageBoxService messageBoxService;
    private readonly ISharedResourceLatch isBusyLatch;
    private readonly ISharedResourceLatch applyLatch;
    private readonly ApplicationUpdateHandlerRegistrator registeredUpdater;
    private readonly ReadOnlyObservableCollection<IReleaseEntry> nativeAvailableReleases;
    private readonly ReadOnlyObservableCollection<IReleaseEntry> emptyAvailableReleases = new(new ObservableCollection<IReleaseEntry>());
    private IPoeUpdateInfo nativeLatestUpdate;
    private IPoeUpdateInfo observedGenericNativeUpdate;
    private bool nativeCanUpdateToLatest;
    private bool nativeHasUpdatesToInstall;
    private bool nativeIsBusy;
    private bool nativeIsInErrorStatus;
    private string nativeStatusText;
    private long updateSourceGeneration;
    private long nativeProjectionGeneration;

    static ApplicationUpdaterViewModel()
    {
        Binder.Bind(x => x.isBusyLatch.IsBusy || x.applyLatch.IsBusy || x.updaterModel.IsBusy || x.RestartCommand.IsBusy || x.ApplyUpdateCommand.IsBusy || x.CheckForUpdatesCommand.IsBusy)
            .To((x, v) => x.IsBusy = v, x => x.uiScheduler);

        Binder.Bind(x => x.LatestUpdate != null)
            .OnScheduler(x => x.uiScheduler)
            .To(x => x.CanUpdateToLatest);

        Binder.Bind(x => x.LatestUpdate)
            .OnScheduler(x => x.uiScheduler)
            .To((x, update) => x.HasUpdatesToInstall = update != null && update.ReleasesToApply.EmptyIfNull().Any());
    }

    public ApplicationUpdaterViewModel(
        IFactory<IUpdaterWindowDisplayer> updateWindowDisplayer,
        IApplicationUpdaterModel updaterModel,
        ApplicationUpdateHandlerRegistrator registeredUpdater,
        IApplicationAccessor applicationAccessor,
        IConfigProvider<UpdateSettingsConfig> configProvider,
        IMessageBoxService messageBoxService,
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
        this.registeredUpdater = registeredUpdater;
        this.messageBoxService = messageBoxService;
        this.uiScheduler = uiScheduler;
        this.isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        applyLatch = new SharedResourceLatch().AddTo(Anchors);

        registeredUpdater.HandedOff += PublishHandoff;
        Disposable.Create(() => registeredUpdater.HandedOff -= PublishHandoff).AddTo(Anchors);

        Observable.FromEvent(handler => registeredUpdater.Changed += handler, handler => registeredUpdater.Changed -= handler)
            .ObserveOn(uiScheduler)
            .SubscribeSafe(() =>
            {
                // HandedOff has its own synchronous dispatcher publication. Queued progress
                // or reentrant source/disposal notifications cannot publish it a second time.
                if (registeredUpdater.Phase == ApplicationUpdatePhase.HandedOff)
                {
                    this.RaisePropertyChanged(nameof(IsInErrorStatus));
                    this.RaisePropertyChanged(nameof(StatusText));
                    return;
                }
                var nativeUpdate = registeredUpdater.NativeUpdate;
                var generation = Interlocked.Read(ref updateSourceGeneration);
                if (nativeUpdate != null && !ReferenceEquals(nativeUpdate, observedGenericNativeUpdate) &&
                    ReferenceEquals(nativeUpdate, updaterModel.LatestUpdate) && HasCurrentNativeProjection())
                {
                    availableReleasesSource.EditDiff(nativeUpdate.RemoteReleases);
                    // A collection consumer may choose a different source or native result synchronously.
                    if (generation != Interlocked.Read(ref updateSourceGeneration) ||
                        ReferenceEquals(nativeUpdate, observedGenericNativeUpdate) ||
                        !ReferenceEquals(nativeUpdate, registeredUpdater.NativeUpdate) ||
                        !ReferenceEquals(nativeUpdate, updaterModel.LatestUpdate)) return;
                    LatestUpdate = nativeUpdate;
                }
                // Forward live getters, never a captured state that could outlive source invalidation.
                this.RaisePropertyChanged(nameof(LatestUpdate));
                this.RaisePropertyChanged(nameof(AvailableReleases));
                this.RaisePropertyChanged(nameof(SelectedUpdate));
                this.RaisePropertyChanged(nameof(InstalledVersionText));
                this.RaisePropertyChanged(nameof(TargetVersionText));
                this.RaisePropertyChanged(nameof(Phase));
                this.RaisePropertyChanged(nameof(CanApplyPreparedUpdate));
                this.RaisePropertyChanged(nameof(ProgressStage));
                this.RaisePropertyChanged(nameof(ProgressPercent));
                this.RaisePropertyChanged(nameof(IsProgressIndeterminate));
                this.RaisePropertyChanged(nameof(IsBusy));
                this.RaisePropertyChanged(nameof(IsInErrorStatus));
                this.RaisePropertyChanged(nameof(StatusText));
            }, Log.HandleUiException)
            .AddTo(Anchors);

        var checkForUpdatesCommandSink = new Subject<Unit>();
        CheckForUpdatesCommand = CommandWrapper
            .Create<object>(async x => { checkForUpdatesCommandSink.OnNext(Unit.Default); }, updaterModel.WhenAnyValue(x => x.UpdateSource).Select(x => x?.IsValid == true));

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
        nativeAvailableReleases = availableReleases;

        updaterModel.WhenAnyValue(x => x.UpdateSource)
            .Select(_ => Interlocked.Increment(ref updateSourceGeneration))
            .ObserveOn(uiScheduler)
            .SubscribeSafe(generation =>
            {
                if (generation != Interlocked.Read(ref updateSourceGeneration)) return;
                LatestUpdate = null;
                CanUpdateToLatest = false;
                HasUpdatesToInstall = false;
                SetStatus(string.Empty);
                availableReleasesSource.Clear();
                // Collection/property consumers may choose another source during cleanup.
                if (generation != Interlocked.Read(ref updateSourceGeneration)) return;
                Interlocked.Exchange(ref nativeProjectionGeneration, generation);

                // Only the cleared UI-owned projection can become current for this source.
                this.RaisePropertyChanged(nameof(LatestUpdate));
                this.RaisePropertyChanged(nameof(CanUpdateToLatest));
                this.RaisePropertyChanged(nameof(HasUpdatesToInstall));
                this.RaisePropertyChanged(nameof(AvailableReleases));
                this.RaisePropertyChanged(nameof(StatusText));
                this.RaisePropertyChanged(nameof(IsInErrorStatus));
                this.RaisePropertyChanged(nameof(ProgressPercent));
            }, Log.HandleUiException)
            .AddTo(Anchors);

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
            .Create(Restart)
            .AddTo(Anchors);

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
            .Where(x => updaterModel.UpdateSource?.IsValid == true)
            .Do(x => Log.Debug($"Checking for updates: {x}"))
            .Where(x => !IsBusy)
            .SubscribeAsync(async x =>
            {
                if (registeredUpdater.RoutesCurrentSource)
                    await registeredUpdater.CheckAsync(CancellationToken.None, preservePrepared: !x.open);
                else
                    await CheckForUpdate(open: x.open, silent: x.silent);
            })
            .AddTo(Anchors);

        ShowUpdaterCommand = CommandWrapper.Create<object>(ShowUpdaterCommandExecuted);
        ApplyUpdateCommand = CommandWrapper.Create<object>(
            x => ApplyUpdateCommandExecuted(applyRelease: x is bool b ? b : true),
            this.WhenAnyValue(x => x.CanUpdateToLatest).ObserveOn(uiScheduler));

        OpenUri = CommandWrapper.Create<string>(OpenUriCommandExecuted);
        Binder.Attach(this).AddTo(Anchors);
    }

    public string UpdateInfo => LatestUpdate?.ReleasesToApply.EmptyIfNull().Select(x => $"{x.Version} (delta: {x.IsDelta})").JoinStrings(" => ");

#nullable enable
    public IApplicationUpdateInfo? SelectedUpdate => registeredUpdater.SelectedUpdate;
    public string InstalledVersionText => registeredUpdater.InstalledVersionText;
    public string TargetVersionText => registeredUpdater.TargetVersionText;
    public ApplicationUpdatePhase Phase => registeredUpdater.Phase;
    public bool CanApplyPreparedUpdate => registeredUpdater.CanApplyPreparedUpdate;
    public string ProgressStage => registeredUpdater.ProgressStage;
    public bool IsProgressIndeterminate => registeredUpdater.IsProgressIndeterminate;

    public Task<UpdateOperationOutcome> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (nativeIsBusy) return Task.FromResult(UpdateOperationOutcome.Busy);
        return registeredUpdater.CheckAsync(cancellationToken);
    }

    public Task<UpdateOperationOutcome> DownloadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (nativeIsBusy) return Task.FromResult(UpdateOperationOutcome.Busy);
        return registeredUpdater.DownloadAsync(cancellationToken);
    }

    /// <summary>Dispatches a prepared target through application-owned installation acceptance.</summary>
    public Task<UpdateOperationOutcome> ApplyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (nativeIsBusy) return Task.FromResult(UpdateOperationOutcome.Busy);
        return registeredUpdater.ApplyAsync(cancellationToken);
    }

    public Task<ApplicationUpdateChangelog> GetChangelogAsync(CultureInfo culture, CancellationToken cancellationToken)
    {
        return registeredUpdater.GetChangelogAsync(culture, cancellationToken);
    }

    private void PublishHandoff()
    {
        this.RaisePropertyChanged(nameof(SelectedUpdate));
        this.RaisePropertyChanged(nameof(InstalledVersionText));
        this.RaisePropertyChanged(nameof(TargetVersionText));
        this.RaisePropertyChanged(nameof(CanApplyPreparedUpdate));
        this.RaisePropertyChanged(nameof(IsBusy));
        this.RaisePropertyChanged(nameof(Phase));
    }
#nullable restore

    public bool IsDeltaUpdate => LatestUpdate?.ReleasesToApply?.EmptyIfNull().Any(x => x.IsDelta) ?? false;

    public long TotalUpdateSize => LatestUpdate?.ReleasesToApply?.EmptyIfNull().Sum(x => x.Filesize) ?? 0;

    public bool IgnoreDeltaUpdates => updaterModel.IgnoreDeltaUpdates;

    public CommandWrapper CheckForUpdatesCommand { get; }

    public CommandWrapper RestartCommand { get; }

    public CommandWrapper ApplyUpdateCommand { get; }

    public CommandWrapper ShowUpdaterCommand { get; }

    // Native setter equality uses its backing value: the public getter can already
    // project the registered owner's state during a legacy setter invocation.
    [PropertyChanged.DoNotNotify]
    public bool IsInErrorStatus
    {
        get => registeredUpdater.UsesGenericProjection ? registeredUpdater.IsInErrorStatus : HasCurrentNativeProjection() && nativeIsInErrorStatus;
        private set
        {
            if (nativeIsInErrorStatus == value) return;
            nativeIsInErrorStatus = value;
            this.RaisePropertyChanged(nameof(IsInErrorStatus));
        }
    }

    // Compare native backing values: a superseded-source getter can already project false
    // while the queued UI operation still needs to clear the old native value.
    [PropertyChanged.DoNotNotify]
    public bool CanUpdateToLatest
    {
        get => HasCurrentNativeProjection() && nativeCanUpdateToLatest;
        [UsedImplicitly] private set
        {
            if (nativeCanUpdateToLatest == value) return;
            nativeCanUpdateToLatest = value;
            this.RaisePropertyChanged(nameof(CanUpdateToLatest));
        }
    }

    [PropertyChanged.DoNotNotify]
    public bool HasUpdatesToInstall
    {
        get => HasCurrentNativeProjection() && nativeHasUpdatesToInstall;
        [UsedImplicitly] private set
        {
            if (nativeHasUpdatesToInstall == value) return;
            nativeHasUpdatesToInstall = value;
            this.RaisePropertyChanged(nameof(HasUpdatesToInstall));
        }
    }

    public bool AutomaticallyDownloadUpdates { get; private set; }

    [PropertyChanged.DoNotNotify]
    public string StatusText
    {
        get => registeredUpdater.UsesGenericProjection ? registeredUpdater.StatusText : HasCurrentNativeProjection() ? nativeStatusText : string.Empty;
        private set
        {
            if (nativeStatusText == value) return;
            nativeStatusText = value;
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public bool IsOpen { get; set; }

    public ReadOnlyObservableCollection<IReleaseEntry> AvailableReleases => HasCurrentNativeProjection() ? nativeAvailableReleases : emptyAvailableReleases;

    public bool CheckForUpdates { get; set; }

    // Preserve backing-value equality even when the public superseded-source projection is already null.
    [PropertyChanged.DoNotNotify]
    public IPoeUpdateInfo LatestUpdate
    {
        get
        {
            if (!HasCurrentNativeProjection()) return null;
            var nativeUpdate = registeredUpdater.NativeUpdate;
            return nativeUpdate != null && !ReferenceEquals(nativeUpdate, observedGenericNativeUpdate) &&
                ReferenceEquals(nativeUpdate, updaterModel.LatestUpdate) ? nativeUpdate : nativeLatestUpdate;
        }
        private set
        {
            // A later legitimate legacy setter wins over an already observed generic native result,
            // including force-update results that do not replace the model's LatestUpdate property.
            observedGenericNativeUpdate = registeredUpdater.NativeUpdate;
            if (Equals(nativeLatestUpdate, value)) return;
            nativeLatestUpdate = value;
            this.RaisePropertyChanged(nameof(LatestUpdate));
        }
    }

    public Version LatestAppliedVersion => updaterModel.LatestAppliedVersion;

    public Version LatestVersion => LatestUpdate?.FutureReleaseEntry?.Version?.Version;

    public UpdateSourceInfo UpdateSource => updaterModel.UpdateSource;

    public int ProgressPercent => registeredUpdater.UsesGenericProjection ? registeredUpdater.ProgressPercent :
        registeredUpdater.NativeProgressPercent ?? (HasCurrentNativeProjection() ? updaterModel.ProgressPercent : 0);

    private bool HasCurrentNativeProjection() => !registeredUpdater.UsesGenericProjection &&
        updaterModel.UpdateSource != null &&
        Interlocked.Read(ref nativeProjectionGeneration) == Interlocked.Read(ref updateSourceGeneration);

    [PropertyChanged.DoNotNotify]
    public bool IsBusy
    {
        get => registeredUpdater.IsBusy || nativeIsBusy;
        private set
        {
            if (nativeIsBusy == value) return;
            nativeIsBusy = value;
            this.RaisePropertyChanged(nameof(IsBusy));
        }
    }

    public CommandWrapper OpenUri { get; }

    public FileInfo LauncherExecutable => updaterModel.LauncherExecutable;

    public Task<IReleaseEntry> FetchUpdate(Version targetVersion)
    {
        return registeredUpdater.TrackNativeOperation(() => updaterModel.CheckForUpdate(targetVersion),
            () => Task.FromException<IReleaseEntry>(new InvalidOperationException("An incompatible operation already owns the updater.")));
    }

    /// <summary>Keeps native force-preparation ownership through its actual task and synchronous notifications.</summary>
    public Task<IPoeUpdateInfo> PrepareForceUpdate(IReleaseEntry targetRelease)
    {
        return registeredUpdater.TrackNativeOperation(() => PrepareForceUpdateCore(targetRelease),
            () => Task.FromException<IPoeUpdateInfo>(new InvalidOperationException("An incompatible operation already owns the updater.")));
    }

    private async Task<IPoeUpdateInfo> PrepareForceUpdateCore(IReleaseEntry targetRelease)
    {
        // Force preparation has no model Busy entry to retire a prior generic Download's progress.
        registeredUpdater.RetireCompletedNativeProgress();
        Log.Debug($"Force update preparation requested, target: {new {targetRelease.Version, targetRelease.Filename, targetRelease.Filesize}}");
        var result = await updaterModel.PrepareForceUpdate(targetRelease);
        LatestUpdate = result;
        updaterModel.Reset();
        SetStatus($"Ready to update to v{LatestVersion}");
        return result;
    }

    /// <summary>Keeps native task ownership independent of queued UI Busy notifications.</summary>
    public Task DownloadUpdate(bool applyAutomatically)
    {
        return registeredUpdater.TrackNativeOperation(() => DownloadUpdateCore(applyAutomatically),
            () => Task.FromException(new InvalidOperationException("An incompatible operation already owns the updater.")));
    }

    private async Task DownloadUpdateCore(bool applyAutomatically)
    {
        Log.Debug($"Applying update {LatestVersion} (updated version: {LatestAppliedVersion})");
        if (applyLatch.IsBusy)
        {
            throw new InvalidOperationException("Another operation is already in progress");
        }

        using var _ = applyLatch.Rent();
        SetStatus($"Preparing to update to v{LatestVersion}...");

        var latestUpdate = LatestUpdate;
        if (latestUpdate == null)
        {
            throw new InvalidOperationException("Latest update must be specified");
        }

        var currentVersion = LatestAppliedVersion;
        var targetVersion = LatestVersion;
        var isDowngrade = currentVersion > targetVersion;
        Log.Info($"Validating update: {currentVersion} => {targetVersion} (downgrade: {isDowngrade})");
        if (isDowngrade)
        {
            if (await messageBoxService.ShowConfirmation("Version downgrade",
                    $"Are you sure you want to update to v{targetVersion} as your current version is newer {currentVersion}?{Environment.NewLine}This may bring compatibility problems and is not recommended.") == false)
            {
                return;
            }
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

            if (!applyAutomatically)
            {
                return;
            }

            SetStatus($"Installing v{LatestVersion}...");
            await updaterModel.ApplyRelease(latestUpdate);
            IsOpen = true;
            SetStatus($"Successfully updated to v{LatestVersion}, restarting...");
            LatestUpdate = default;
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

    public async Task Restart()
    {
        Log.Debug("Restart application requested");

        try
        {
            IsOpen = true;
            SetStatus("Restarting application...");

            updaterModel.RestartApplicationViaLauncher();
        }
        catch (Exception ex)
        {
            IsOpen = true;

            Log.HandleUiException(ex);
            SetError($"Failed to restart application - {ex.Message}");
        }
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

        var checkGeneration = Interlocked.Read(ref updateSourceGeneration);
        Log.Debug($"Update check requested, source: {updaterModel.UpdateSource}");
        if (Phase != ApplicationUpdatePhase.ReadyToApply) SetStatus("Checking for updates...");

        if (open)
        {
            IsOpen = true;
            await Task.Delay(UiConstants.ArtificialShortDelay);
        }

        if (checkGeneration != Interlocked.Read(ref updateSourceGeneration)) return;
        try
        {
            var sw = Stopwatch.StartNew();
            var outcome = await registeredUpdater.CheckAsync(CancellationToken.None, preservePrepared: !open);
            if (checkGeneration != Interlocked.Read(ref updateSourceGeneration)) return;
            if (outcome != UpdateOperationOutcome.Completed || Phase == ApplicationUpdatePhase.ReadyToApply) return;
            var selectedTarget = SelectedUpdate;

            var timeToWait = sw.Elapsed - UiConstants.ArtificialShortDelay;
            if (timeToWait > TimeSpan.Zero)
            {
                // delaying update so the user could see the progress ring
                await Task.Delay(UiConstants.ArtificialShortDelay);
            }

            if (checkGeneration != Interlocked.Read(ref updateSourceGeneration) || !ReferenceEquals(selectedTarget, SelectedUpdate)) return;
            var newVersion = registeredUpdater.NativeUpdate;
            if (newVersion == null)
            {
                throw new ApplicationException($"Failed to get latest update info");
            }

            availableReleasesSource.EditDiff(newVersion.RemoteReleases);
            // Public collection consumers may change the source synchronously.
            if (checkGeneration != Interlocked.Read(ref updateSourceGeneration) || !ReferenceEquals(selectedTarget, SelectedUpdate)) return;
            LatestUpdate = newVersion;
            if (newVersion.ReleasesToApply.Any())
            {
                SetStatus($"New version v{LatestVersion} is available");
                if (AutomaticallyDownloadUpdates && checkGeneration == Interlocked.Read(ref updateSourceGeneration) &&
                    selectedTarget != null && ReferenceEquals(selectedTarget, SelectedUpdate) && Phase == ApplicationUpdatePhase.UpdateAvailable)
                {
                    SetStatus($"Downloading v{LatestVersion} ({ByteSize.FromBytes(newVersion.ReleasesToApply.EmptyIfNull().Sum(x => x.Filesize)).MegaBytes:F0} MB)...");
                    if (checkGeneration != Interlocked.Read(ref updateSourceGeneration) || !ReferenceEquals(selectedTarget, SelectedUpdate)) return;
                    var download = await DownloadAsync(CancellationToken.None);
                    if (download == UpdateOperationOutcome.Completed && checkGeneration == Interlocked.Read(ref updateSourceGeneration) &&
                        ReferenceEquals(selectedTarget, SelectedUpdate) && Phase == ApplicationUpdatePhase.ReadyToApply)
                        SetStatus($"Downloaded v{LatestVersion}");
                }
            }
            else
            {
                SetStatus($"No updates in {updaterModel.UpdateSource.Name} channel, you're using the latest version");
            }
        }
        catch (Exception ex)
        {
            if (checkGeneration != Interlocked.Read(ref updateSourceGeneration)) return;
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
        await DownloadUpdate(applyRelease);
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
}
