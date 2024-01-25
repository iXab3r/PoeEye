using System.Collections.Immutable;
using System.Reflection;

namespace PoeShared.Services;

public interface IAssemblyTracker
{
    private static readonly Lazy<AssemblyTracker> InstanceSupplier = new(() => new AssemblyTracker());

    public static IAssemblyTracker Instance => InstanceSupplier.Value;

    IReadOnlyReactiveList<Assembly> Assemblies { get; }
}