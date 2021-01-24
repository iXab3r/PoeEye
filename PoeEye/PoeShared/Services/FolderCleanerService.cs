using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;
using ObservableEx = PoeShared.Scaffolding.ObservableEx;

namespace PoeShared.Services
{
    internal sealed class FolderCleanerService : DisposableReactiveObject, IFolderCleanerService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FolderCleanerService));

        private readonly SourceList<DirectoryInfo> directoriesSource = new SourceList<DirectoryInfo>();
        private TimeSpan? fileTimeToLive;
        private TimeSpan? cleanupTimeout;

        public FolderCleanerService(IClock clock)
        {
            directoriesSource
                .Connect()
                .Bind(out var targetDirectories)
                .Subscribe()
                .AddTo(Anchors);
            TargetDirectories = targetDirectories;

            Observable.Merge(
                    this.WhenAnyValue(x => x.FileTimeToLive, x => x.CleanupTimeout).ToUnit(),
                    directoriesSource.Connect().CountChanged().ToUnit())
                .Select(_ =>
                {
                    Log.Debug($"Cleanup period updated to {cleanupTimeout} with TTL set to {fileTimeToLive} with {directoriesSource.Count} target directories");
                    return cleanupTimeout > TimeSpan.Zero && fileTimeToLive > TimeSpan.Zero && directoriesSource.Count > 0 ? ObservableEx.BlockingTimer(cleanupTimeout.Value) : Observable.Never<long>();
                })
                .Switch()
                .Subscribe(() => HandleCleanupTimerTick(clock, fileTimeToLive ?? TimeSpan.MaxValue, directoriesSource.Items.ToArray()))
                .AddTo(Anchors);
        }

        private static void HandleCleanupTimerTick(IClock clock, TimeSpan ttl, DirectoryInfo[] directories)
        {
            using var sw = new BenchmarkTimer($"Cleanup, TTL: {ttl}", Log);
            try
            {
                if (ttl <= TimeSpan.Zero)
                {
                    sw.Step($"File TTL is not set, value: {ttl}");
                    return;
                }

                sw.Step($"Starting cleanup cycle, TTL :{ttl.TotalDays:F0} days, directory list: {directories.DumpToTextRaw()}");
                foreach (var directoryInfo in directories)
                {
                    sw.Step($"[{directoryInfo}] Processing directory...");
                    try
                    {
                        directoryInfo.Refresh();
                        if (!directoryInfo.Exists)
                        {
                            sw.Step($"Directory {directoryInfo} does not exist - skipping");
                        }
                        else
                        {
                            var allFiles = directoryInfo.EnumerateFiles().ToArray();
                            var filesToRemove = allFiles.Where(x => CheckThatFileMustBeRemoved(x, clock, ttl)).ToArray();

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
                                    Log.Warn($"Failed to remove file {fileInfo.Name}", e);
                                    sw.Step($"[{directoryInfo}] Failed to remove file {fileInfo.Name} - {e.Message}");
                                }
                            }
                        
                            sw.Step($"[{directoryInfo}] Cleaned up {totalCleanedSize}b, {filesToRemove.Length} / {allFiles.Length} were removed or processed");
                        }
                    
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to process directory {directoryInfo}", e);
                        sw.Step($"[{directoryInfo}] Failed to process - {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("Exception in cleanup thread", e);
                sw.Step($"Exception occurred - {e.Message}");
            }
            finally{
            {
                sw.Step("Cleanup cycle completed");
            }}
        }

        private static bool CheckThatFileMustBeRemoved(FileInfo fileInfo, IClock clock, TimeSpan ttl)
        {
            if (!fileInfo.Exists || fileInfo.IsReadOnly || ttl <= TimeSpan.Zero)
            {
                return false;
            }

            var utcNow = clock.UtcNow;
            var writeExpired = utcNow - fileInfo.LastWriteTimeUtc > ttl;
            return writeExpired;
        }

        public ReadOnlyObservableCollection<DirectoryInfo> TargetDirectories { get; }

        public TimeSpan? FileTimeToLive
        {
            get => fileTimeToLive;
            set => RaiseAndSetIfChanged(ref fileTimeToLive, value);
        }

        public TimeSpan? CleanupTimeout
        {
            get => cleanupTimeout;
            set => RaiseAndSetIfChanged(ref cleanupTimeout, value);
        }

        public IDisposable AddDirectory(DirectoryInfo directoryInfo)
        {
            Log.Debug($"Adding directory {directoryInfo} to directory list: {directoriesSource.DumpToTextRaw()}");
            directoriesSource.Add(directoryInfo);
            return Disposable.Create(() =>
            {
                Log.Debug($"Removing directory {directoryInfo} from directory list: {directoriesSource.DumpToTextRaw()}");
                directoriesSource.Remove(directoryInfo);
            });
        }
    }
}