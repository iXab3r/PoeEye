using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Core;
using Squirrel;

namespace PoeShared.Squirrel.Updater;

public interface IApplicationUpdaterViewModel : IDisposableReactiveObject
{
    CommandWrapper CheckForUpdatesCommand { get; }

    CommandWrapper RestartCommand { get; }

    CommandWrapper ApplyUpdateCommand { get; }
        
    CommandWrapper ShowUpdaterCommand { get; }

    bool IsInErrorStatus { get; }
    
    bool CanUpdateToLatest { get; }
    
    bool HasUpdatesToInstall { get; }

    string StatusText { get; }
        
    int ProgressPercent { get; }

    bool IsOpen { get; set; }
        
    bool IsBusy { get; }

    UpdateSourceInfo UpdateSource { get; }
        
    Version LatestAppliedVersion { get; }

    Version LatestVersion { get; }
        
    IPoeUpdateInfo LatestUpdate { get; }
        
    ReadOnlyObservableCollection<IReleaseEntry> AvailableReleases { get; }

    bool CheckForUpdates { get; set; }

    [NotNull] CommandWrapper OpenUri { get; }
    
    FileInfo LauncherExecutable { get; }

    Task PrepareForceUpdate(IReleaseEntry targetRelease);
}