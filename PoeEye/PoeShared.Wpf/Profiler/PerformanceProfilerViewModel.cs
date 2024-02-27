using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Profiler;

internal sealed class PerformanceProfilerViewModel : DisposableReactiveObjectWithLogger, IProfilerViewModel
{
    private static readonly Binder<PerformanceProfilerViewModel> Binder = new();
    private readonly IProfilerService profilerService;
    private readonly IScheduler uiScheduler;

    static PerformanceProfilerViewModel()
    {
        Binder.Bind(x => x.profilerService.IsBusy).OnScheduler(x => x.uiScheduler).To(x => x.IsBusy);
        Binder.Bind(x => x.profilerService.IsRunning).OnScheduler(x => x.uiScheduler).To(x => x.IsRunning);
        Binder.Bind(x => x.profilerService.IsCollecting).OnScheduler(x => x.uiScheduler).To(x => x.IsCollecting);
    }

    public PerformanceProfilerViewModel(
        IProfilerService profilerService)
    {
        this.profilerService = profilerService;

        Log.Info($"Registering traces report provider");
        uiScheduler = DispatcherScheduler.Current;
        StartProfilingCommand = CommandWrapper.Create(StartProfilingExecuted, this.WhenAnyValue(x => x.IsCollecting).Select(x => IsCollecting == false));
        StopProfilingCommand = CommandWrapper.Create(StopCollectingExecuted, this.WhenAnyValue(x => x.IsRunning).Select(x => IsRunning));
        TakeMemorySnapshotCommand = CommandWrapper.Create(TakeMemorySnapshotCommandExecuted);
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsBusy { get; [UsedImplicitly] private set; }

    public bool IsRunning { get; [UsedImplicitly] private set; }

    public bool IsCollecting { get; [UsedImplicitly] private set; }

    public ICommand StopProfilingCommand { get; }

    public ICommand StartProfilingCommand { get; }

    public ICommand TakeMemorySnapshotCommand { get; }

    private async Task TakeMemorySnapshotCommandExecuted()
    {
        await profilerService.TakeMemorySnapshot();
    }

    private async Task StopCollectingExecuted()
    {
        await profilerService.StopCollecting();
    }

    private async Task StartProfilingExecuted()
    {
        await profilerService.StartProfiling();
    }
}