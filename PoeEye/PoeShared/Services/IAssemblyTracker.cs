using System.Reflection;
using DynamicData;

namespace PoeShared.Services;

public interface IAssemblyTracker
{
    private static readonly Lazy<AssemblyTracker> InstanceSupplier = new(() => new AssemblyTracker());

    public static IAssemblyTracker Instance => InstanceSupplier.Value;
    
    IObservableList<Assembly> LoadedAssemblies { get; }
    
    IObservable<Assembly> WhenLoaded { get; }
}