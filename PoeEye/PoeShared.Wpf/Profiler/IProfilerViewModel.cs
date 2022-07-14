using System.Windows.Input;

namespace PoeShared.Profiler;

public interface IProfilerViewModel
{
    bool IsBusy { get; }
    
    bool IsRunning { get; }
    
    bool IsCollecting { get; }
    
    ICommand StopProfilingCommand { get; }
    
    ICommand StartProfilingCommand { get; }
}