using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using Common.Logging;
using PoeEye;

namespace PoeShared.Modularity
{
    public sealed class PoeEyeConfigProviderInMemory : IConfigProvider
    {
        private static readonly ILog Log = LogManager.GetLogger<PoeEyeConfigProviderInMemory>();

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
        private readonly ConcurrentDictionary<Type, IPoeEyeConfig> loadedConfigs = new ConcurrentDictionary<Type, IPoeEyeConfig>();

        public PoeEyeConfigProviderInMemory()
        {
            if (AppArguments.Instance.IsDebugMode)
            {
                Log.Debug("[PoeEyeConfigProviderInMemory..ctor] Debug mode detected");
            }
            else
            {
                throw new ApplicationException("InMemory config must be used only in debug mode");
            }
        }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        public void Reload()
        {
            Log.Debug($"[PoeEyeConfigProviderInMemory.Reload] Reloading configuration...");

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

            return (TConfig)loadedConfigs.GetOrAdd(typeof(TConfig), key => (TConfig)Activator.CreateInstance(typeof(TConfig)));
        }
    }
}