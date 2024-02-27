using System.Threading.Tasks;

namespace PoeShared.Profiler;

public interface IProfilerService
{
    bool IsBusy { get; }
    
    bool IsRunning { get; }
    
    bool IsCollecting { get; }
    
    Task StopCollecting();

    Task StartProfiling();

    Task TakeMemorySnapshot();
}