using System.Linq.Expressions;
using System.Reactive;
using DynamicData;
using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IConfigProvider<TConfig>
    where TConfig : IPoeEyeConfig, new()
{
    TConfig ActualConfig { [NotNull] get; }

    IObservable<TConfig> WhenChanged { [NotNull] get; }

    void Save([NotNull] TConfig config);

    [NotNull]
    IObservable<T> ListenTo<T>([NotNull] Expression<Func<TConfig, T>> fieldToMonitor);
}

public interface IConfigProvider 
{
    IObservable<Unit> ConfigHasChanged { [NotNull] get; }
    
    IObservableCache<IPoeEyeConfig, string> Configs { get; }

    void Save();

    void Save(IPoeEyeConfig config);

    TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new();
}