using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Core;
using Squirrel;

namespace PoeShared.Squirrel.Updater;

public interface IApplicationUpdaterViewModel : IDisposableReactiveObject
{
#nullable enable
    /// <summary>The opaque target of this updater cycle, or null when no target is selected.</summary>
    IApplicationUpdateInfo? SelectedUpdate { get; }

    string InstalledVersionText { get; }
    string TargetVersionText { get; }
    ApplicationUpdatePhase Phase { get; }
    bool CanApplyPreparedUpdate { get; }
    string ProgressStage { get; }

    /// <summary>When true, the current stage has no measured percentage; the legacy numeric value is not displayed.</summary>
    bool IsProgressIndeterminate { get; }

    /// <summary>Checks the selected source; cancellation throws, while source invalidation returns Invalidated.</summary>
    Task<UpdateOperationOutcome> CheckAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Prepares the available target selected by Check without installing it; incompatible concurrent operations return Busy.
    /// A configured registered channel requires a selected target before Download can start.
    /// </summary>
    Task<UpdateOperationOutcome> DownloadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Accepts one prepared local plan after the product helper acknowledges ownership.
    /// Caller cancellation applies before acceptance and drains actual work. After acceptance the fixed
    /// plan outlives caller/author disposal and source changes. The synchronous HandedOff notification
    /// precedes parent shutdown; acceptance does not prove replacement or successor readiness.
    /// </summary>
    Task<UpdateOperationOutcome> ApplyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads notes for the available target selected by Check without occupying the mutation Busy slot.
    /// Superseded target/source/language requests cancel after handler drain without a UI error.
    /// </summary>
    /// <exception cref="InvalidOperationException">No available target has been selected for the registered source.</exception>
    Task<ApplicationUpdateChangelog> GetChangelogAsync(CultureInfo culture, CancellationToken cancellationToken);
#nullable restore

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

    /// <summary>
    /// Returns the native check's actual Task, or a faulted Task with InvalidOperationException
    /// while an author Download or any Apply owns the updater. Occupancy does not throw synchronously or queue a check.
    /// </summary>
    Task<IReleaseEntry> FetchUpdate(Version targetVersion);
    
    /// <summary>
    /// Prepares a native release while retaining its operation lifetime independently of queued UI notifications.
    /// While an author Download or any Apply owns the updater, returns a faulted Task with InvalidOperationException
    /// without throwing synchronously, starting native preparation or queuing work.
    /// </summary>
    Task<IPoeUpdateInfo> PrepareForceUpdate(IReleaseEntry targetRelease);
    
    Task DownloadUpdate(bool applyAutomatically);

    Task Restart();
}
