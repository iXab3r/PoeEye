using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using PoeEye;

namespace PoeShared.Modularity
{
    public sealed class PoeEyeConfigProviderInMemory : IConfigProvider
    {
        private readonly ConcurrentDictionary<Type, IPoeEyeConfig> loadedConfigs = new ConcurrentDictionary<Type, IPoeEyeConfig>();

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();

        public PoeEyeConfigProviderInMemory()
        {
            if (AppArguments.Instance.IsDebugMode)
            {
                Log.Instance.Debug("[PoeEyeConfigProviderInMemory..ctor] Debug mode detected");
            }
            else
            {
                throw new ApplicationException("InMemory config must be used only in debug mode");
            }
        }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        public void Reload()
        {
            Log.Instance.Debug($"[PoeEyeConfigProviderInMemory.Reload] Reloading configuration...");

            loadedConfigs.Clear();

            configHasChanged.OnNext(Unit.Default);
        }
        
        public void Save<TConfig>(TConfig config) where TConfig : IPoeEyeConfig, new()
        {
            loadedConfigs[typeof(TConfig)] = config;
        }

        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            if (loadedConfigs.IsEmpty)
            {
                Reload();
            }
            return (TConfig)loadedConfigs.GetOrAdd(typeof(TConfig), (key) => (TConfig)Activator.CreateInstance(typeof(TConfig)));
        }
    }
}