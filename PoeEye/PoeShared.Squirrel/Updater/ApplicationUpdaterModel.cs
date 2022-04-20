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
        Binder
            .Bind(x => new DirectoryInfo(Path.Combine(x.RootDirectory.FullName, x.appArguments.AppName)))
            .To(x => x.AppRootDirectory);
    }
 
    public ApplicationUpdaterModel(
        IApplicationAccessor applicationAccessor,
        IUpdateSourceProvider updateSourceProvider,
        IAppArguments appArguments)
    {
        this.applicationAccessor = applicationAccessor;
        this.appArguments = appArguments;

        MostRecentVersionAppFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        RootDirectory = new DirectoryInfo(Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%"));
            
        updateSourceProvider
            .WhenAnyValue(x => x.UpdateSource)
            .WithPrevious()
            .SubscribeSafe(x =>
            {
                Log.Debug(() => $"Update source changed: {x}");
                UpdateSource = x.Current;
            }, Log.HandleUiException)
            .AddTo(Anchors);
            
        var currentProcessName = Process.GetCurrentProcess().ProcessName + ".exe";
        Log.Debug(() => $"Initializing ApplicationName, processName: {currentProcessName}, appArguments executable: {appArguments.ApplicationExecutableName}");
        ApplicationExecutableFileName = $"{appArguments.AppName}.exe";
        Log.Debug(() => $"Application will be started via executing {ApplicationExecutableFileName}");
        Binder.Attach(this).AddTo(Anchors);
    }

    public string ApplicationExecutableFileName { get; }
    
    public DirectoryInfo RootDirectory { get; }
    
    public DirectoryInfo AppRootDirectory { get; [UsedImplicitly] private set; }

    public DirectoryInfo MostRecentVersionAppFolder { get; set; }

    public UpdateSourceInfo UpdateSource { get; private set; }

    public bool IgnoreDeltaUpdates { get; set; }

    public Version LatestAppliedVersion { get; private set; }

    public IPoeUpdateInfo LatestUpdate { get; private set; }

    public int ProgressPercent { get; private set; }

    public bool IsBusy { get; private set; }

    public async Task ApplyRelease(IPoeUpdateInfo updateInfo)
    {
        Guard.ArgumentNotNull(updateInfo, nameof(updateInfo));

        Log.Debug(() => $"Applying update {updateInfo}");

        using var unused = CreateIsBusyAnchor();
        using var mgr = await CreateManager();
            
        Log.Debug("Downloading releases...");

        const string downloadReleaseTaskName = "DownloadRelease";
        const string applyReleaseTaskName = "ApplyRelease";
        var progressByTask = new Dictionary<string, int>{ { downloadReleaseTaskName, 0 }, { applyReleaseTaskName, 0 } };
        void CombinedProgressReporter(int progressPercent, string taskName)
        {
            progressByTask[taskName] = progressPercent;
            var totalProgress = (int)progressByTask.Values.Average();
            UpdateProgress(totalProgress, $"{taskName} {progressPercent}%");
        }

        var downloadedReleases = await mgr.DownloadReleases(updateInfo.ReleasesToApply, x => CombinedProgressReporter(x, downloadReleaseTaskName));
        Log.Warn($"Downloaded following releases:\n\t{downloadedReleases.DumpToTable()}");

        string newVersionFolder;
        if (appArguments.IsDebugMode)
        {
            Log.Warn("Debug mode detected, simulating update process without touching files");
            newVersionFolder = AppDomain.CurrentDomain.BaseDirectory;
            for (var i = 0; i < 20; i++)
            {
                CombinedProgressReporter(i*5, downloadReleaseTaskName);
                await Task.Delay(500);
            }
            for (var i = 0; i < 10; i++)
            {
                CombinedProgressReporter(i*10, applyReleaseTaskName);
                await Task.Delay(500);
            }
        }
        else
        {
            Log.Debug(() => $"Applying releases: {updateInfo}");
            newVersionFolder = await mgr.ApplyReleases(updateInfo, x => CombinedProgressReporter(x, applyReleaseTaskName));
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
        Log.Debug(() => $"Force update to {new { releaseEntry.Version, releaseEntry.Filename, releaseEntry.Filesize }} requested");

        using var mgr = await CreateManager();
        var updateInfo = await mgr.PrepareUpdate(IgnoreDeltaUpdates, ArraySegment<IReleaseEntry>.Empty, new[] { releaseEntry });
        Log.Debug(() => $"Force UpdateInfo:\r\n{updateInfo.Dump().TakeChars(300)}");
        return updateInfo;
    }

    /// <summary>
    ///     Checks whether update exist and if so, downloads it
    /// </summary>
    /// <returns>True if application was updated</returns>
    public async Task CheckForUpdates()
    {
        Log.Debug(() => "Update check requested");
        using var unused = CreateIsBusyAnchor();
        Reset();

        using var mgr = await CreateManager();
        Log.Debug(() => $"Checking for updates @ {UpdateSource}, {nameof(IgnoreDeltaUpdates)}: {IgnoreDeltaUpdates}...");

        var updateInfo = await mgr.CheckForUpdate(IgnoreDeltaUpdates, CheckUpdateProgress);
        Log.Debug(() => $"UpdateInfo:\r\n{updateInfo.Dump().TakeChars(300)}");
        LatestUpdate = updateInfo;
    }

    public async Task RestartApplication()
    {
        // NB: Here's how this method works:
        //
        // 1. We're going to pass the *name* of our EXE and the params to 
        //    Update.exe
        // 2. Update.exe is going to grab our PID (via getting its parent), 
        //    then wait for us to exit.
        // 3. We exit cleanly, dropping any single-instance mutexes or 
        //    whatever.
        // 4. Update.exe unblocks, then we launch the app again, possibly 
        //    launching a different version than we started with (this is why
        //    we take the app's *name* rather than a full path)
        using var unused = CreateIsBusyAnchor();
            
        var executable = GetLatestExecutable();
        Log.Debug(
            $"Restarting app, folder: {MostRecentVersionAppFolder}, appName: {ApplicationExecutableFileName}, {executable}...");

        var appPath = Path.Combine(MostRecentVersionAppFolder.FullName, ApplicationExecutableFileName);
        var appArgs = new StringBuilder();
        if (appArguments.IsDebugMode)
        {
            appArgs.Append($" -d");
        }

        Log.Debug(() => $"Starting application @ '{appPath}', args: {appArgs} ...");
        var updaterProcess = Process.Start(appPath, appArgs.ToString());
        if (updaterProcess == null)
        {
            throw new FileNotFoundException($"Failed to start application @ '{appPath}'");
        }
        Log.Debug(() => $"Process spawned, PID: {updaterProcess.Id}");
        await applicationAccessor.Exit();
    }

    public FileInfo GetLatestExecutable()
    {
        var appExecutable = new FileInfo(Path.Combine(AppRootDirectory.FullName, ApplicationExecutableFileName));
        Log.Debug(() => $"Application executable: {appExecutable} (exists: {appExecutable.Exists})");
        if (!appExecutable.Exists)
        {
            throw new FileNotFoundException("Application executable was not found", appExecutable.FullName);
        }
        return appExecutable;
    }

    private async Task<IPoeUpdateManager> CreateManager()
    {
        var manager = new ResilientUpdateManager(
            UpdateSource.Uris,
            async uri =>
            {
                Log.Debug(() => $"Creating manager for URL {uri}");
                var manager = await CreateManager(uri);
                Log.Debug(() => $"Created manager: {manager}");
                return manager;
            }
        );
        return manager;
    }
    private async Task<IPoeUpdateManager> CreateManager(string updateUrl)
    {
        Log.Debug(() => $"Using update source: {UpdateSource.Dump()}");

        var downloader = new BasicAuthFileDownloader(
            new NetworkCredential(
                UpdateSource.Username?.ToUnsecuredString(),
                UpdateSource.Password?.ToUnsecuredString()));
        if (updateUrl.Contains("github"))
        {
            Log.Debug(() => $"Using GitHub source: {UpdateSource.Dump()}");

            var mgr = await PoeUpdateManager.GitHubUpdateManager(
                updateUrl,
                downloader,
                appArguments.AppName,
                RootDirectory.FullName);
            return mgr;
        }
        else
        {
            Log.Debug(() => $"Using BasicHTTP source: {UpdateSource.Dump()}");
            var mgr = new PoeUpdateManager(
                updateUrl, 
                downloader,
                appArguments.AppName, 
                RootDirectory.FullName);
            return mgr;
        }
    }

    private void UpdateProgress(int progressPercent, string taskName)
    {
        Log.Debug(() => $"{taskName} is in progress: {progressPercent}%");
        ProgressPercent = progressPercent;
    }

    private void CheckUpdateProgress(int progressPercent)
    {
        Log.Debug(() => $"Check update is in progress: {progressPercent}%");
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