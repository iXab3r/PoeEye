using System.Reactive.Subjects;
using System.Reflection;
using DynamicData;
using PoeShared.Modularity;
using ReactiveUI;

namespace PoeShared.Services;

internal sealed class AssemblyTracker : DisposableReactiveObjectWithLogger, IAssemblyTracker
{
    private readonly ISourceList<Assembly> loadedAssemblies = new SourceList<Assembly>();
    private readonly ReplaySubject<Assembly> loadedSink = new();

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
            .Subscribe(assembly =>
            {
                var assemblyName = assembly.GetName();
                if (assemblyName.Name.StartsWith("ℛ*") || assemblyName.Name.StartsWith("Microsoft.GeneratedCod"))
                {
                    Log.Debug($"Assembly is loaded, but not tracked, reason - blacklist: {assembly}");
                    // these are dynamically emitted assemblies
                    return;
                }

                loadedAssemblies.Add(assembly);
            })
            .AddTo(Anchors);

        loadedAssemblies
            .Connect()
            .OnItemAdded(assembly =>
            {
                Log.Debug($"Assembly is now tracked: {assembly}");
                loadedSink.OnNext(assembly);
            })
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
    }

    public IObservableList<Assembly> LoadedAssemblies => loadedAssemblies;

    public IObservable<Assembly> WhenLoaded => loadedSink.AsObservable();
}