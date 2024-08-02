using System;
using System.IO;
using System.Threading.Tasks;
using ByteSizeLib;
using JetBrains.Profiler.SelfApi;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;

namespace PoeShared.Profiler;

internal sealed class PerformanceProfiler : DisposableReactiveObjectWithLogger, IProfilerService
{
    private readonly IUniqueIdGenerator idGenerator;
    private static readonly Binder<PerformanceProfiler> Binder = new();

    static PerformanceProfiler()
    {
    }

    public PerformanceProfiler(
        IExceptionReportingService exceptionReportingService,
        TraceSnapshotReportProvider traceSnapshotReportProvider,
        IUniqueIdGenerator idGenerator)
    {
        this.idGenerator = idGenerator;
        TracesFolder = traceSnapshotReportProvider.TracesFolder;

        Log.Info($"Registering traces report provider");
        exceptionReportingService.AddReportItemProvider(traceSnapshotReportProvider);

        Binder.Attach(this).AddTo(Anchors);
    }

    public DirectoryInfo TracesFolder { get; }

    public bool IsBusy { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsCollecting { get; private set; }

    public async Task TakeMemorySnapshot()
    {
        Log.Info($"Saving memory snapshot");
        Log.Info($"Checking memory profiler prerequisites");
        await DotMemory.EnsurePrerequisiteAsync();
        Log.Info($"Memory profiler prerequisites checked");
        
        EnsureTracesDirectoryExists();

        Log.Info($"Awaiting for data collection start");
        await Task.Run(() =>
        {
            var newSnapshotName = Path.Combine(TracesFolder.FullName, $"Memory-PID{Environment.ProcessId}-{idGenerator.Next()}");
            Log.Info($"Starting collecting memory data to file {newSnapshotName}");
            var config = new DotMemory.Config().SaveToFile(newSnapshotName, overwrite: true); 
            var snapshotFilePath = DotMemory.GetSnapshotOnce(config);
            Log.Info($"Memory profiler returned: {snapshotFilePath}");
            var snapshotFile = new FileInfo(snapshotFilePath);
            if (!snapshotFile.Exists)
            {
                Log.Warn($"Detached memory profiler, snapshot: {snapshotFilePath} - does not exist");
                throw new FileNotFoundException($"Snapshot operation failed - memory dump file was not created");
            }
            else
            {
                Log.Info($"Detached memory profiler, snapshot: {snapshotFilePath} (exists: {snapshotFile.Exists}, size: {new ByteSize(snapshotFile.Length)})");
            }
        });
    }

    public async Task StopCollecting()
    {
        if (IsCollecting)
        {
            Log.Info($"Saving profiler data");
            DotTrace.SaveData();
            Log.Info($"Awaiting for profiler data archive");
            await Task.Run(() =>
            {
                Log.Info($"Saving profiler data archive");
                var snapshotFilesArchive = DotTrace.GetCollectedSnapshotFilesArchive(true);
                Log.Info($"Profiler provide the following data archive: {snapshotFilesArchive}");
                var newSnapshotName = Path.Combine(Path.GetDirectoryName(snapshotFilesArchive) ?? string.Empty, $"Performance-PID{Environment.ProcessId}-{Path.GetFileName(snapshotFilesArchive)}");
                File.Move(snapshotFilesArchive, newSnapshotName);
                Log.Info($"Snapshot data archive: {newSnapshotName}");
            });
            IsCollecting = false; // after SaveData profiler stops collection automatically
        }

        Log.Info($"Awaiting profiler Detach");
        await Task.Run(() =>
        {
            Log.Info($"Detaching profiler");
            DotTrace.Detach();
            Log.Info($"Detached profiler");
        });
        IsRunning = false;
        Log.Info($"Stopped profiling");
    }

    public async Task StartProfiling()
    {
        if (!IsRunning)
        {
            try
            {
                IsBusy = true;
                Log.Info($"Checking prerequisites");
                await DotTrace.EnsurePrerequisiteAsync();
                Log.Info($"Prerequisites checked");

                EnsureTracesDirectoryExists();
                var config = new DotTrace.Config()
                    .SaveToDir(TracesFolder.FullName);

                Log.Info($"Starting profiling using config: {config}");
                await Task.Run(() =>
                {
                    Log.Info($"Attaching profiler");
                    DotTrace.Attach(config);
                    Log.Info($"Attached profiler");
                });
                Log.Info($"Successfully attached profiler");
                IsRunning = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        Log.Info($"Awaiting for data collection start");
        await Task.Run(() =>
        {
            Log.Info($"Starting collecting data");
            DotTrace.StartCollectingData();
            Log.Info($"Detached profiler");
            Log.Info($"Started collecting data");
        });
        IsCollecting = true;
        Log.Info($"Started profiling session");
    }

    private void EnsureTracesDirectoryExists()
    {
        TracesFolder.Refresh();
        if (TracesFolder.Exists)
        {
            Log.Info($"Directory for traces: {TracesFolder.FullName}");
        }
        else
        {
            Log.Info($"Creating directory for traces: {TracesFolder.FullName}");
            TracesFolder.Create();
        }
    }
}