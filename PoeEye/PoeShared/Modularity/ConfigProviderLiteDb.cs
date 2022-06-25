using System.Reactive;
using System.Reactive.Subjects;
using DynamicData;

namespace PoeShared.Modularity;

public sealed class ConfigProviderLiteDb : DisposableReactiveObject, IConfigProvider
{
    private static readonly string DatabaseFileName = @"config.db";
    
    public IObservable<Unit> ConfigHasChanged { get; }
    public IObservableCache<IPoeEyeConfig, string> Configs { get; }

    private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();

    public IDisposable RegisterStrategy(IConfigProviderStrategy strategy)
    {
        return Disposable.Empty;
    }

    public void Reload()
    {
        throw new NotImplementedException();
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Save(IPoeEyeConfig config)
    {
        throw new NotImplementedException();
    }

    public void SaveToFile(FileInfo file)
    {
        throw new NotImplementedException();
    }

    public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
    {
        throw new NotImplementedException();
    }
}