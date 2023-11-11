using System.Reflection;
using DynamicData;

namespace PoeShared.Services;

public interface IAssemblyTracker
{
    IObservableList<Assembly> LoadedAssemblies { get; }
    
    IObservable<Assembly> WhenLoaded { get; }
}