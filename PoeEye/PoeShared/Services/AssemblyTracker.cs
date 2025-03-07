using System.Reflection;

namespace PoeShared.Services;

internal sealed class AssemblyTracker : DisposableReactiveObjectWithLogger, IAssemblyTracker
{
    private readonly ReactiveList<Assembly> loadedAssemblies = new();

    public AssemblyTracker()
    {
        var domain = AppDomain.CurrentDomain;
        Log.AddSuffix($"AppDomain {domain.FriendlyName}");
        Log.Info($"Assembly tracker is created for app domain {new { domain, domain.Id, domain.BaseDirectory, domain.IsFullyTrusted, domain.MonitoringTotalAllocatedMemorySize, domain.MonitoringTotalProcessorTime }}");   

        var loadedAssembliesSource = Observable
            .Defer(() => domain.GetAssemblies().ToObservable());
        var loadedAfterStartupSource = Observable
            .FromEventPattern<AssemblyLoadEventHandler, AssemblyLoadEventArgs>(h => domain.AssemblyLoad += h, h => domain.AssemblyLoad -= h)
            .Select(x => x.EventArgs.LoadedAssembly);

        loadedAssembliesSource
            .Concat(loadedAfterStartupSource)
            .Where(assembly =>
            {
                var assemblyName = assembly.GetName();
                if (assemblyName.Name.StartsWith("ℛ*") || //legacy REPL assemblies
                    assemblyName.Name.StartsWith("ℛ_") || 
                    assemblyName.Name.StartsWith("Microsoft.GeneratedCod"))
                {
                    Log.Debug($"Assembly is loaded, but not tracked, reason - blacklist: {assembly}");
                    // these are dynamically emitted assemblies
                    return false;
                }

                return true;
            })
            .Subscribe(assembly =>
            {
                Log.Debug($"Assembly is now tracked: {assembly}");
                loadedAssemblies.Add(assembly);
            })
            .AddTo(Anchors);
    }

    public IReadOnlyReactiveList<Assembly> Assemblies => loadedAssemblies;
}