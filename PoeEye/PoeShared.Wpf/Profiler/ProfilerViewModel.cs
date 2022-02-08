using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Profiler.SelfApi;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Profiler;

internal sealed class ProfilerViewModel : DisposableReactiveObjectWithLogger, IProfilerViewModel
{
    private static readonly Binder<ProfilerViewModel> Binder = new();

    static ProfilerViewModel()
    {
    }

    public ProfilerViewModel(
        IExceptionReportingService exceptionReportingService,
        TraceSnapshotReportProvider traceSnapshotReportProvider,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        TracesFolder = traceSnapshotReportProvider.TracesFolder;

        Log.Info(() => $"Registering traces report provider");
        exceptionReportingService.AddReportItemProvider(traceSnapshotReportProvider);

        StartProfilingCommand = CommandWrapper.Create(StartProfilingExecuted, this.WhenAnyValue(x => x.IsCollecting).Select(x => IsCollecting == false).ObserveOn(uiScheduler));
        StopProfilingCommand = CommandWrapper.Create(StopCollectingExecuted, this.WhenAnyValue(x => x.IsRunning).Select(x => IsRunning).ObserveOn(uiScheduler));
        Binder.Attach(this).AddTo(Anchors);
    }
    
    public DirectoryInfo TracesFolder { get; }
    
    public bool IsBusy { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsCollecting { get; private set; }

    public ICommand StopProfilingCommand { get; }

    public ICommand StartProfilingCommand { get; }

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
                Log.Info(() => $"Profiler provide the following data archive: {snapshotFilesArchive}");
                var newSnapshotName = Path.Combine(Path.GetDirectoryName(snapshotFilesArchive) ?? string.Empty, $"Performance-PID{Environment.ProcessId}-{Path.GetFileName(snapshotFilesArchive)}");
                File.Move(snapshotFilesArchive, newSnapshotName);
                Log.Info(() => $"Snapshot data archive: {newSnapshotName}");
            });
            IsCollecting = false; // after SaveData profiler stops collection automatically
        }

        Log.Info($"Awaiting profiler Detach");
        await Task.Run(() =>
        {
            Log.Info(() => $"Detaching profiler");
            DotTrace.Detach();
            Log.Info(() => $"Detached profiler");
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
                Log.Info(() => $"Checking prerequisites");
                await DotTrace.EnsurePrerequisiteAsync();
                Log.Info(() => $"Prerequisites checked");

                TracesFolder.Refresh();
                if (TracesFolder.Exists)
                {
                    Log.Info(() => $"Directory for traces: {TracesFolder.FullName}");
                }
                else
                {
                    Log.Info(() => $"Creating directory for traces: {TracesFolder.FullName}");
                    TracesFolder.Create();
                }

                var config = new DotTrace.Config()
                    .SaveToDir(TracesFolder.FullName);

                Log.Info(() => $"Starting profiling using config: {config}");
                await Task.Run(() =>
                {
                    Log.Info(() => $"Attaching profiler");
                    DotTrace.Attach(config);
                    Log.Info(() => $"Attached profiler");
                });
                Log.Info(() => $"Successfully attached profiler");
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
            Log.Info(() => $"Detached profiler");
            Log.Info($"Started collecting data");
        });
        IsCollecting = true;
        Log.Info($"Started profiling session");
    }
}