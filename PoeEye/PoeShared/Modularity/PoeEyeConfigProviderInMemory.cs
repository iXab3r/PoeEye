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

    public PoeEyeConfigProviderInMemory(IAppArguments appArguments)
    {
        Guard.ArgumentNotNull(appArguments, nameof(appArguments));
            
        if (appArguments.IsDebugMode)
        {
            Log.Debug(() => $"[PoeEyeConfigProviderInMemory..ctor] Debug mode detected");
        }
        else
        {
            throw new ApplicationException($"InMemory config must be used only in debug mode, args: {appArguments.Dump()}");
        }
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