using System.Reactive.Subjects;
using System.Reflection;
using DynamicData;
using PoeShared.Modularity;

namespace PoeShared.Services;

internal sealed class AssemblyTracker : DisposableReactiveObjectWithLogger, IAssemblyTracker
{
    private readonly ISourceCache<Assembly, Assembly> loadedCache = new SourceCache<Assembly, Assembly>(x => x);
    private readonly ReplaySubject<Assembly> loadedSink = new();

    public AssemblyTracker()
    {
        var domain = AppDomain.CurrentDomain;
        Observable
            .FromEventPattern<AssemblyLoadEventHandler, AssemblyLoadEventArgs>(h => domain.AssemblyLoad += h, h => domain.AssemblyLoad -= h)
            .Select(x => x.EventArgs.LoadedAssembly)
            .Subscribe(assembly =>
            {
                if (assembly.FullName == null || assembly.FullName.StartsWith("ℛ*"))
                {
                    // these are dynamically emitted assemblies by CSS
                    return;
                }

                loadedCache.AddOrUpdate(assembly);
            })
            .AddTo(Anchors);
        loadedCache.AddOrUpdate(domain.GetAssemblies());

        loadedCache
            .Connect()
            .OnItemAdded(assembly =>
            {
                Log.Debug(() => $"Assembly is now tracked: {assembly}");
                loadedSink.OnNext(assembly);
            })
            .BindToCollection(out var loaded)
            .Subscribe()
            .AddTo(Anchors);
        LoadedAssemblies = loaded;
    }

    public IReadOnlyObservableCollection<Assembly> LoadedAssemblies { get; }
    
    public IObservable<Assembly> WhenLoaded => loadedSink.AsObservable();
}