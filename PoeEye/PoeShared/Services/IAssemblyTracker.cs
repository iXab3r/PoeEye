using System.Reflection;

namespace PoeShared.Services;

public interface IAssemblyTracker
{
    IReadOnlyObservableCollection<Assembly> LoadedAssemblies { get; }
    
    IObservable<Assembly> WhenLoaded { get; }
}