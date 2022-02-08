using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.Profiler;

public interface IProfilerViewModel : IDisposableReactiveObject
{
    bool IsRunning { get; }
    
    bool IsCollecting { get; }
    
    ICommand StopProfilingCommand { get; }
    
    ICommand StartProfilingCommand { get; }
}