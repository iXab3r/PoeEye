using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ByteSizeLib;
using JetBrains.Profiler.SelfApi;
using PoeShared.Dialogs.Services;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Profiler;

internal sealed class PerformanceProfilerViewModel : DisposableReactiveObjectWithLogger, IProfilerViewModel
{
    private readonly IUniqueIdGenerator idGenerator;
    private readonly IMessageBoxService messageBoxService;
    private static readonly Binder<PerformanceProfilerViewModel> Binder = new();

    static PerformanceProfilerViewModel()
    {
    }

    public PerformanceProfilerViewModel(
        IExceptionReportingService exceptionReportingService,
        TraceSnapshotReportProvider traceSnapshotReportProvider,
        IUniqueIdGenerator idGenerator,
        IMessageBoxService messageBoxService,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        this.idGenerator = idGenerator;
        this.messageBoxService = messageBoxService;
        TracesFolder = traceSnapshotReportProvider.TracesFolder;

        Log.Info($"Registering traces report provider");
        exceptionReportingService.AddReportItemProvider(traceSnapshotReportProvider);

        StartProfilingCommand = CommandWrapper.Create(StartProfilingExecuted, this.WhenAnyValue(x => x.IsCollecting).Select(x => IsCollecting == false).ObserveOn(uiScheduler));
        StopProfilingCommand = CommandWrapper.Create(StopCollectingExecuted, this.WhenAnyValue(x => x.IsRunning).Select(x => IsRunning).ObserveOn(uiScheduler));
        TakeMemorySnapshotCommand = CommandWrapper.Create(TakeMemorySnapshotCommandExecuted);
        Binder.Attach(this).AddTo(Anchors);
    }

    public DirectoryInfo TracesFolder { get; }

    public bool IsBusy { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsCollecting { get; private set; }

    public ICommand StopProfilingCommand { get; }

    public ICommand StartProfilingCommand { get; }

    public ICommand TakeMemorySnapshotCommand { get; }

    private async Task TakeMemorySnapshotCommandExecuted()
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
            Log.Info($"Detached memory profiler, snapshot: {snapshotFilePath} (exists: {snapshotFile.Exists}, size: {new ByteSize(snapshotFile.Length)})");
        });
    }

    private async Task StopCollectingExecuted()
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

    private async Task StartProfilingExecuted()
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