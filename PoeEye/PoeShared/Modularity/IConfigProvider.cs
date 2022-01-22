using System;
using System.IO;
using System.Linq.Expressions;
using System.Reactive;
using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IConfigProvider<TConfig>
    where TConfig : IPoeEyeConfig
{
    TConfig ActualConfig { [NotNull] get; }

    IObservable<TConfig> WhenChanged { [NotNull] get; }

    void Reload();

    void Save([NotNull] TConfig config);

    [NotNull]
    IObservable<T> ListenTo<T>([NotNull] Expression<Func<TConfig, T>> fieldToMonitor);
}

public interface IConfigProvider
{
    IObservable<Unit> ConfigHasChanged { [NotNull] get; }

    IDisposable RegisterStrategy([NotNull] IConfigProviderStrategy strategy);
        
    void Reload();

    void Save();
        
    void Save<TConfig>([NotNull] TConfig config) where TConfig : IPoeEyeConfig, new();

    void SaveToFile(FileInfo file);

    TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new();
}