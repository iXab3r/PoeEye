using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;
using PoeShared.Squirrel.Core;
using PropertyBinder;
using ReactiveUI;
using Squirrel;

namespace PoeShared.Squirrel.Updater;

internal sealed class ApplicationUpdaterModel : DisposableReactiveObject, IApplicationUpdaterModel
{
    private static readonly IFluentLog Log = typeof(ApplicationUpdaterModel).PrepareLogger();
    private static readonly Binder<ApplicationUpdaterModel> Binder = new();

    private static readonly string DotnetCoreRunnerName = "dotnet.exe";
    private static readonly string UpdaterExecutableName = "update.exe";
    private readonly IAppArguments appArguments;
    private readonly IApplicationAccessor applicationAccessor;

    static ApplicationUpdaterModel()
    {
    }
 
    public ApplicationUpdaterModel(
        IApplicationAccessor applicationAccessor,
        IUpdateSourceProvider updateSourceProvider,
        IAppArguments appArguments)
    {
        this.applicationAccessor = applicationAccessor;
        this.appArguments = appArguments;

        MostRecentVersionAppFolder = new DirectoryInfo(appArguments.AppDomainDirectory);
        AppRootDirectory = new DirectoryInfo(appArguments.LocalAppDataDirectory);
        RootDirectory = AppRootDirectory.Parent;
            
        updateSourceProvider
            .WhenAnyValue(x => x.UpdateSource)
            .WithPrevious()
            .SubscribeSafe(x =>
            {
                Log.Debug($"Update source changed: {x}");
                UpdateSource = x.Current;
            }, Log.HandleUiException)
            .AddTo(Anchors);
            
        Log.Debug($"Initializing ApplicationName, process path: {Environment.ProcessPath}, appArguments executable: {appArguments.ApplicationExecutableName}");
        RunningExecutable = new FileInfo(Environment.ProcessPath ?? throw new InvalidStateException("Process path must be defined"));
        LauncherExecutable = new FileInfo(Path.Combine(AppRootDirectory.FullName, $"{appArguments.AppName}.exe"));
        Log.Debug($"Application startup data: { new { Environment.ProcessPath, appArguments.ApplicationExecutableName, AppRootDirectory, RunningExecutable, LauncherExecutable } }");
        Binder.Attach(this).AddTo(Anchors);
    }

    public DirectoryInfo RootDirectory { get; }
    
    public FileInfo RunningExecutable { get; }
    
    public FileInfo LauncherExecutable { get; }

    public DirectoryInfo AppRootDirectory { get; }

    public DirectoryInfo MostRecentVersionAppFolder { get; set; }

    public UpdateSourceInfo UpdateSource { get; private set; }

    public bool IgnoreDeltaUpdates { get; set; }

    public Version LatestAppliedVersion { get; private set; }

    public IPoeUpdateInfo LatestUpdate { get; private set; }

    public int ProgressPercent { get; private set; }

    public bool IsBusy { get; private set; }
    
    public async Task<bool> VerifyRelease(IPoeUpdateInfo updateInfo)
    {
        Guard.ArgumentNotNull(updateInfo, nameof(updateInfo));
        
        using var unused = CreateIsBusyAnchor();
        using var mgr = await CreateManager();
        using var progressTracker = new ComplexProgressTracker();
        using var progressUpdater = progressTracker.WhenAnyValue(x => x.ProgressPercent).Subscribe(x => ProgressPercent = x);

        Log.Debug($"Verifying update files {updateInfo}");
        var verificationResult = await mgr.VerifyReleases(updateInfo.ReleasesToApply, x => progressTracker.Update(x, "VerifyRelease"));
        Log.Debug($"Verification result: {verificationResult}, update: {updateInfo}");
        return verificationResult;
    }

    public async Task DownloadRelease(IPoeUpdateInfo updateInfo)
    {
        Guard.ArgumentNotNull(updateInfo, nameof(updateInfo));
        
        using var unused = CreateIsBusyAnchor();
        using var mgr = await CreateManager();
        using var progressTracker = new ComplexProgressTracker();
        using var progressUpdater = progressTracker.WhenAnyValue(x => x.ProgressPercent).Subscribe(x => ProgressPercent = x);

        Log.Debug($"Downloading release, update {updateInfo}");
        var downloadedReleases = await mgr.DownloadReleases(updateInfo.ReleasesToApply, x => progressTracker.Update(x, "DownloadRelease"));
        Log.Warn($"Downloaded following releases:\n\t{downloadedReleases.DumpToTable()}");
    }
    
    public async Task ApplyRelease(IPoeUpdateInfo updateInfo)
    {
        Guard.ArgumentNotNull(updateInfo, nameof(updateInfo));

        Log.Debug($"Applying update {updateInfo}");

        using var unused = CreateIsBusyAnchor();
        using var mgr = await CreateManager();
        using var progressTracker = new ComplexProgressTracker();
        using var progressUpdater = progressTracker.WhenAnyValue(x => x.ProgressPercent).Subscribe(x => ProgressPercent = x);
        
        Log.Debug("Downloading releases...");

        const string applyReleaseTaskName = "ApplyRelease";

        string newVersionFolder;
        if (appArguments.IsDebugMode)
        {
            Log.Warn("Debug mode detected, simulating update process without touching files");
            newVersionFolder = appArguments.AppDomainDirectory;
            for (var i = 0; i < 10; i++)
            {
                progressTracker.Update(i*10, applyReleaseTaskName);
                await Task.Delay(500);
            }
        }
        else
        {
            Log.Debug($"Applying releases: {updateInfo}");
            newVersionFolder = await mgr.ApplyReleases(updateInfo, x => progressTracker.Update(x, applyReleaseTaskName));
        }

        var lastAppliedRelease = updateInfo.ReleasesToApply.Last();

        Log.Debug(
            $"Update completed to v{lastAppliedRelease.Version}, result: {newVersionFolder}");

        if (string.IsNullOrWhiteSpace(newVersionFolder))
        {
            throw new ApplicationException("Expected non-empty new version folder path");
        }

        MostRecentVersionAppFolder = new DirectoryInfo(newVersionFolder);
        LatestAppliedVersion = lastAppliedRelease.Version.Version;
        LatestUpdate = null;
        ProgressPercent = 0;
    }

    public void Reset()
    {
        LatestUpdate = null;
        LatestAppliedVersion = null;
        IsBusy = false;
        ProgressPercent = 0;
    }

    public async Task<IPoeUpdateInfo> PrepareForceUpdate(IReleaseEntry releaseEntry)
    {
        Log.Debug($"Force update to {new { releaseEntry.Version, releaseEntry.Filename, releaseEntry.Filesize }} requested");

        using var mgr = await CreateManager();
        var updateInfo = await mgr.PrepareUpdate(IgnoreDeltaUpdates, ArraySegment<IReleaseEntry>.Empty, new[] { releaseEntry });
        Log.Debug($"Force UpdateInfo:\r\n{updateInfo.Dump().TakeChars(300)}");
        return updateInfo;
    }

    /// <summary>
    ///     Checks whether update exist and if so, downloads it
    /// </summary>
    /// <returns>True if application was updated</returns>
    public async Task CheckForUpdates()
    {
        Log.Debug("Update check requested");
        using var unused = CreateIsBusyAnchor();
        Reset();

        using var mgr = await CreateManager();
        Log.Debug($"Checking for updates @ {UpdateSource}, {nameof(IgnoreDeltaUpdates)}: {IgnoreDeltaUpdates}...");

        var updateInfo = await mgr.CheckForUpdate(IgnoreDeltaUpdates, CheckUpdateProgress);
        Log.Debug($"UpdateInfo:\r\n{updateInfo.Dump().TakeChars(300)}");
        LatestUpdate = updateInfo;
    }

    public Task RestartApplication()
    {
        return Restart(RunningExecutable);
    }

    public Task RestartApplicationViaLauncher()
    {
        return Restart(LauncherExecutable);
    }

    private async Task Restart(FileInfo executable)
    {
        using var unused = CreateIsBusyAnchor();
        Log.Debug($"Starting application @ '{executable.FullName}', args: {appArguments.StartupArgs} ...");
        applicationAccessor.RestartAs(executable.FullName, appArguments.StartupArgs);
    }

    private async Task<IPoeUpdateManager> CreateManager()
    {
        var manager = new ResilientUpdateManager(
            UpdateSource.Uris,
            async uri =>
            {
                Log.Debug($"Creating manager for URL {uri}");
                var manager = await CreateManager(uri);
                Log.Debug($"Created manager: {manager}");
                return manager;
            }
        );
        return manager;
    }
    private async Task<IPoeUpdateManager> CreateManager(string updateUrl)
    {
        Log.Debug($"Using update source: {UpdateSource.Dump()}");

        var downloader = new BasicAuthFileDownloader(
            new NetworkCredential(
                UpdateSource.Username?.ToUnsecuredString(),
                UpdateSource.Password?.ToUnsecuredString()));
        if (updateUrl.Contains("github"))
        {
            Log.Debug($"Using GitHub source: {UpdateSource.Dump()}");

            var mgr = await PoeUpdateManager.GitHubUpdateManager(
                updateUrl,
                downloader,
                appArguments.AppName,
                RootDirectory.FullName);
            return mgr;
        }
        else
        {
            Log.Debug($"Using BasicHTTP source: {UpdateSource.Dump()}");
            var mgr = new PoeUpdateManager(
                updateUrl, 
                downloader,
                appArguments.AppName, 
                RootDirectory.FullName);
            return mgr;
        }
    }

    private void CheckUpdateProgress(int progressPercent)
    {
        Log.Debug($"Check update is in progress: {progressPercent}%");
    }

    private IDisposable CreateIsBusyAnchor()
    {
        IsBusy = true;
        ProgressPercent = 0;

        return Disposable.Create(
            () =>
            {
                IsBusy = false;
                ProgressPercent = 0;
            });
    }

   
}