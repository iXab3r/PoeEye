using System.Reflection;

namespace PoeShared.Services;

internal interface IAssemblyTracker
{
    IReadOnlyObservableCollection<Assembly> LoadedAssemblies { get; }
    
    IObservable<Assembly> WhenLoaded { get; }
}