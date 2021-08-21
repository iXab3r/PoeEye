using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Modularity
{
    public sealed class PoeEyeConfigProviderInMemory : IConfigProvider
    {
        private static readonly IFluentLog Log = typeof(PoeEyeConfigProviderInMemory).PrepareLogger();

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
        private readonly ConcurrentDictionary<Type, IPoeEyeConfig> loadedConfigs = new ConcurrentDictionary<Type, IPoeEyeConfig>();

        public PoeEyeConfigProviderInMemory(IAppArguments appArguments)
        {
            Guard.ArgumentNotNull(appArguments, nameof(appArguments));
            
            if (appArguments.IsDebugMode)
            {
                Log.Debug($"[PoeEyeConfigProviderInMemory..ctor] Debug mode detected");
            }
            else
            {
                throw new ApplicationException($"InMemory config must be used only in debug mode, args: {appArguments.DumpToTextRaw()}");
            }
        }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        public IDisposable RegisterStrategy(IConfigProviderStrategy strategy)
        {
            return Disposable.Empty;
        }

        public void Reload()
        {
            Log.Debug($"[PoeEyeConfigProviderInMemory.Reload] Reloading configuration...");

            loadedConfigs.Clear();

            configHasChanged.OnNext(Unit.Default);
        }

        public void Save()
        {
            configHasChanged.OnNext(Unit.Default);
        }

        public void Save<TConfig>(TConfig config) where TConfig : IPoeEyeConfig, new()
        {
            loadedConfigs[typeof(TConfig)] = config;
            Save();
        }

        public void SaveToFile(FileInfo file)
        {
            throw new NotSupportedException();
        }

        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            return (TConfig) loadedConfigs.GetOrAdd(typeof(TConfig), key => (TConfig) Activator.CreateInstance(typeof(TConfig)));
        }
    }
}