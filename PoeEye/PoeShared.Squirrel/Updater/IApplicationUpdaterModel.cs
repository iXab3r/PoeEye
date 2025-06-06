using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Squirrel.Core;
using Squirrel;

namespace PoeShared.Squirrel.Updater;

public interface IApplicationUpdaterModel : IDisposableReactiveObject
{
    UpdateSourceInfo UpdateSource { get; }

    bool IgnoreDeltaUpdates { get; set; }

    [CanBeNull] Version LatestAppliedVersion { get; }

    [CanBeNull] IPoeUpdateInfo LatestUpdate { get; }
        
    int ProgressPercent { get; }
        
    bool IsBusy { get; }
    
    FileInfo RunningExecutable { get; }
    
    /// <summary>
    ///   Entry-point of Squirrel app, usually it's located inside folder %LOCALAPPDATA%\appName and has the same name as main executable
    /// </summary>
    FileInfo LauncherExecutable { get; }

    Task CheckForUpdates();

    Task<IReleaseEntry> CheckForUpdate(Version targetVersion);
    
    Task<bool> VerifyRelease(IPoeUpdateInfo updateInfo);

    void RestartApplication();
    
    void RestartApplicationViaLauncher();

    Task DownloadRelease([NotNull] IPoeUpdateInfo updateInfo);
    
    Task ApplyRelease([NotNull] IPoeUpdateInfo updateInfo);

    void Reset();

    Task<IPoeUpdateInfo> PrepareForceUpdate(IReleaseEntry releaseEntry);
}