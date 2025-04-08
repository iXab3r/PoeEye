using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using DynamicData;

namespace PoeShared.Modularity;

public sealed class PoeEyeConfigProviderInMemory : IConfigProvider
{
    private static readonly IFluentLog Log = typeof(PoeEyeConfigProviderInMemory).PrepareLogger();

    private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
    private readonly SourceCache<IPoeEyeConfig, string> loadedConfigs = new(ConfigProviderUtils.GetConfigName);

    public PoeEyeConfigProviderInMemory()
    {
    }

    public IObservableCache<IPoeEyeConfig, string> Configs => loadedConfigs;

    public IObservable<Unit> ConfigHasChanged => configHasChanged;

    public void Save()
    {
        configHasChanged.OnNext(Unit.Default);
    }

    public void Save(IPoeEyeConfig config)
    {
        loadedConfigs.AddOrUpdate(config);
        Save();
    }

    public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
    {
        return (TConfig) loadedConfigs.GetOrAdd(
            ConfigProviderUtils.GetConfigName(typeof(TConfig)), 
            key => (TConfig) Activator.CreateInstance(typeof(TConfig)));
    }
}