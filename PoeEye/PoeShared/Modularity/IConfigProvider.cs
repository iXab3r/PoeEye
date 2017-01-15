using System;
using System.ComponentModel;
using System.Reactive;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PoeShared.Modularity
{
    public interface IConfigProvider<TConfig> : INotifyPropertyChanged
        where TConfig : IPoeEyeConfig
    {
        TConfig ActualConfig { [NotNull] get; }

        void Reload();

        void Save([NotNull] TConfig config);
    }

    public interface IConfigProvider
    {
        void Reload();

        void Save();

        TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new();

        IObservable<Unit> ConfigHasChanged { [NotNull] get; }

        void RegisterConverter([NotNull] JsonConverter converter);
    }
}