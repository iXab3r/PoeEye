using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;

namespace PoeShared.Profiler;

internal sealed class TraceSnapshotReportProvider : DisposableReactiveObject, IExceptionReportItemProvider
{
    private static readonly string TracesFolderName = "traces";
    private static readonly IFluentLog Log = typeof(TraceSnapshotReportProvider).PrepareLogger();

    public TraceSnapshotReportProvider(
        IFolderCleanerService cleanupService,
        IAppArguments appArguments)
    {
        TracesFolder = new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, TracesFolderName));
        
        Log.Debug($"Initializing traces housekeeping for folder {TracesFolder}");
        cleanupService.AddDirectory(TracesFolder).AddTo(Anchors);
        cleanupService.CleanupTimeout = TimeSpan.FromHours(12);
        cleanupService.FileTimeToLive = TimeSpan.FromDays(1);
    }

    public DirectoryInfo TracesFolder { get; }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        return PrepareMemory(outputDirectory).Concat(PrepareTraces(outputDirectory));
    }

    public IEnumerable<ExceptionReportItem> PrepareMemory(DirectoryInfo outputDirectory)
    {
        const int maxSnapshotsToInclude = 5;
        const int snapshotsToAttach = 1;
        Log.Debug("Preparing memory snapshots for report...");
        var snapshotsToInclude = (TracesFolder.Exists 
                ? TracesFolder.GetFiles("*.dmw", SearchOption.TopDirectoryOnly) 
                : Array.Empty<FileInfo>())
            .OrderByDescending(x => x.LastWriteTime)
            .Take(maxSnapshotsToInclude)
            .ToArray();
        Log.Debug($"{TracesFolder} contains the following files: {snapshotsToInclude.Select(y => new { y.Name, y.LastWriteTime }).DumpToTable()}");

        var result = new List<ExceptionReportItem>();
        for (var idx = 0; idx < snapshotsToInclude.Length; idx++)
        {
            var snapshotFile = snapshotsToInclude[idx];
            var snapshotFileName = snapshotFile.FullName.Substring(TracesFolder.FullName.Length).TrimStart('\\', '/');
            var destinationFileName = Path.Combine(outputDirectory.FullName, snapshotFileName);
            try
            {
                Log.Debug(() => $"Copying {snapshotFile.FullName} ({snapshotFile.Length}b) to {destinationFileName}");

                var destinationDirectory = Path.GetDirectoryName(destinationFileName);
                if (destinationDirectory == null)
                {
                    Log.Warn($"Failed to get directory path from destination file name {destinationFileName}");
                    continue;
                }

                Directory.CreateDirectory(destinationDirectory);
                snapshotFile.CopyTo(destinationFileName, true);
                new ExceptionReportItem
                {
                    Description = $"Memory snapshot\n Created: {snapshotFile.CreationTime}\nLast Modified: {snapshotFile.LastWriteTime}",
                    Attachment = new FileInfo(destinationFileName),
                    Attached = idx < snapshotsToAttach
                }.AddTo(result);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to copy log file {snapshotFile} to {destinationFileName}", e);
            }
        }

        foreach (var exceptionReportItem in result)
        {
            yield return exceptionReportItem;
        }
    }
    
    public IEnumerable<ExceptionReportItem> PrepareTraces(DirectoryInfo outputDirectory)
    {
        const int maxSnapshotsToInclude = 5;
        const int snapshotsToAttach = 1;
        Log.Debug("Preparing performance snapshots for report...");
        var snapshotsToInclude = (TracesFolder.Exists 
                ? TracesFolder.GetFiles("*.zip", SearchOption.TopDirectoryOnly) 
                : Array.Empty<FileInfo>())
            .OrderByDescending(x => x.LastWriteTime)
            .Take(maxSnapshotsToInclude)
            .ToArray();
        Log.Debug($"{TracesFolder} contains the following files: {snapshotsToInclude.Select(y => new { y.Name, y.LastWriteTime }).DumpToTable()}");

        var result = new List<ExceptionReportItem>();
        for (var idx = 0; idx < snapshotsToInclude.Length; idx++)
        {
            var snapshotFile = snapshotsToInclude[idx];
            var snapshotFileName = snapshotFile.FullName.Substring(TracesFolder.FullName.Length).TrimStart('\\', '/');
            var destinationFileName = Path.Combine(outputDirectory.FullName, snapshotFileName);
            try
            {
                Log.Debug(() => $"Copying {snapshotFile.FullName} ({snapshotFile.Length}b) to {destinationFileName}");

                var destinationDirectory = Path.GetDirectoryName(destinationFileName);
                if (destinationDirectory == null)
                {
                    Log.Warn($"Failed to get directory path from destination file name {destinationFileName}");
                    continue;
                }

                Directory.CreateDirectory(destinationDirectory);
                snapshotFile.CopyTo(destinationFileName, true);
                new ExceptionReportItem
                {
                    Description = $"Performance snapshot\n Created: {snapshotFile.CreationTime}\nLast Modified: {snapshotFile.LastWriteTime}",
                    Attachment = new FileInfo(destinationFileName),
                    Attached = idx < snapshotsToAttach
                }.AddTo(result);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to copy log file {snapshotFile} to {destinationFileName}", e);
            }
        }

        foreach (var exceptionReportItem in result)
        {
            yield return exceptionReportItem;
        }
    }
}