using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace PoeShared.Services;

internal sealed class FolderCleaner : DisposableReactiveObject, IFolderCleaner
{
    private readonly SourceList<DirectoryCleanupSettings> directoriesSource = new();

    public FolderCleaner(IClock clock)
    {
        Log = typeof(FolderCleaner).PrepareLogger("FolderCleaner");
        Log.AddSuffix(() => $"FC {(directoriesSource.Count == 1 ? $"{directoriesSource.Items.FirstOrDefault()?.Directory?.FullName}" : $"dirs: {directoriesSource.Count}")}");
        directoriesSource
            .Connect()
            .Transform(x => x.Directory)
            .Bind(out var targetDirectories)
            .Subscribe()
            .AddTo(Anchors);
        TargetDirectories = targetDirectories;

        Observable.Merge(
                this.WhenAnyValue(x => x.FileTimeToLive, x => x.FileAccessTimeToLive, x => x.CleanupTimeout).ToUnit(),
                directoriesSource.Connect().CountChanged().ToUnit())
            .Select(_ =>
            {
                Log.Debug($"Cleanup period updated to {CleanupTimeout} with defaults WriteTTL={FileTimeToLive}, AccessTTL={FileAccessTimeToLive} with {directoriesSource.Count} target directories");
                var hasAnyTtl = (FileTimeToLive > TimeSpan.Zero) || (FileAccessTimeToLive > TimeSpan.Zero) || directoriesSource.Items.Any(s => (s.WriteTimeToLive > TimeSpan.Zero) || (s.AccessTimeToLive > TimeSpan.Zero));
                return CleanupTimeout > TimeSpan.Zero && hasAnyTtl && directoriesSource.Count > 0 ? Observables.BlockingTimer(CleanupTimeout.Value, timerName: "Housekeeping") : Observable.Never<long>();
            })
            .Switch()
            .Subscribe(() => HandleCleanupTimerTick(Log, clock, FileTimeToLive, FileAccessTimeToLive, directoriesSource.Items.ToArray()))
            .AddTo(Anchors);
    }
    
    public IFluentLog Log { get; }

    private static void HandleCleanupTimerTick(IFluentLog log, IClock clock, TimeSpan? defaultWriteTtl, TimeSpan? defaultAccessTtl, DirectoryCleanupSettings[] directories)
    {
        using var sw = new BenchmarkTimer($"Cleanup, Default Write TTL: {defaultWriteTtl}, Default Access TTL: {defaultAccessTtl}", log);
        try
        {
            if ((defaultWriteTtl <= TimeSpan.Zero && defaultAccessTtl <= TimeSpan.Zero) && directories.All(d => d.WriteTimeToLive <= TimeSpan.Zero && d.AccessTimeToLive <= TimeSpan.Zero))
            {
                sw.Step($"No TTLs are set, skipping cleanup");
                return;
            }

            sw.Step($"Starting cleanup cycle, defaults: write={defaultWriteTtl}, access={defaultAccessTtl}, directory list: {directories.Select(x => x.Directory).DumpToString()}");
            foreach (var settings in directories)
            {
                var directoryInfo = settings.Directory;
                var writeTtl = settings.WriteTimeToLive ?? defaultWriteTtl ?? TimeSpan.Zero;
                var accessTtl = settings.AccessTimeToLive ?? defaultAccessTtl ?? TimeSpan.Zero;
                sw.Step($"[{directoryInfo}] Processing directory with TTLs: write={writeTtl}, access={accessTtl}...");
                try
                {
                    directoryInfo.Refresh();
                    if (!directoryInfo.Exists)
                    {
                        sw.Step($"Directory {directoryInfo} does not exist - skipping");
                    }
                    else
                    {
                        var allFiles = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
                        var filesToRemove = allFiles.Where(x => CheckThatFileMustBeRemoved(x, clock, writeTtl, accessTtl)).ToArray();

                        var totalCleanedSize = 0L;
                        foreach (var fileInfo in filesToRemove)
                        {
                            try
                            {
                                var fileSize = fileInfo.Length;
                                fileInfo.Delete();
                                sw.Step($"[{directoryInfo}] Removed file {fileInfo.Name} ({fileSize}b)");
                                totalCleanedSize += fileSize;
                            }
                            catch (Exception e)
                            {
                                log.Warn($"Failed to remove file {fileInfo.Name}", e);
                                sw.Step($"[{directoryInfo}] Failed to remove file {fileInfo.Name} - {e.Message}");
                            }
                        }

                        sw.Step($"[{directoryInfo}] Cleaned up {totalCleanedSize}b, {filesToRemove.Length} / {allFiles.Length} were removed or processed");
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Failed to process directory {directoryInfo}", e);
                    sw.Step($"[{directoryInfo}] Failed to process - {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            log.Warn($"Exception in cleanup thread", e);
            sw.Step($"Exception occurred - {e.Message}");
        }
        finally
        {
            {
                sw.Step("Cleanup cycle completed");
            }
        }
    }

    private static bool CheckThatFileMustBeRemoved(FileInfo fileInfo, IClock clock, TimeSpan writeTtl, TimeSpan accessTtl)
    {
        if (!fileInfo.Exists || fileInfo.IsReadOnly)
        {
            return false;
        }

        var utcNow = clock.UtcNow;
        var writeExpired = writeTtl > TimeSpan.Zero && (utcNow - fileInfo.LastWriteTimeUtc > writeTtl);
        var accessExpired = accessTtl > TimeSpan.Zero && (utcNow - fileInfo.LastAccessTimeUtc > accessTtl);
        return writeExpired || accessExpired;
    }

    public ReadOnlyObservableCollection<DirectoryInfo> TargetDirectories { get; }

    public string Name { get; set; }
    public TimeSpan? FileTimeToLive { get; set; }
    public TimeSpan? FileAccessTimeToLive { get; set; }

    public TimeSpan? CleanupTimeout { get; set; }

    public IDisposable AddDirectory(DirectoryInfo directoryInfo)
    {
        return AddDirectory(new DirectoryCleanupSettings(directoryInfo));
    }

    public IDisposable AddDirectory(DirectoryCleanupSettings settings)
    {
        Log.Debug($"Adding directory {settings.Directory} to directory list: {directoriesSource.Items.Select(x => x.Directory).DumpToString()}");
        directoriesSource.Add(settings);
        return Disposable.Create(() =>
        {
            Log.Debug($"Removing directory {settings.Directory} from directory list: {directoriesSource.Items.Select(x => x.Directory).DumpToString()}");
            directoriesSource.Remove(settings);
        });
    }
}