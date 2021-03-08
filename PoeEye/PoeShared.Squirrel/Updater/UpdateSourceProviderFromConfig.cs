using System.Collections.Generic;
using System.Collections.ObjectModel;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class UpdateSourceProviderFromConfig : DisposableReactiveObject, IUpdateSourceProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdateSourceProviderFromConfig));

        private readonly IConfigProvider<UpdateSettingsConfig> configProvider;
        private readonly ISourceCache<UpdateSourceInfo, string> knownSources = new SourceCache<UpdateSourceInfo, string>(x => x.Uri);
        private UpdateSourceInfo updateSource;

        public UpdateSourceProviderFromConfig(IConfigProvider<UpdateSettingsConfig> configProvider)
        {
            Log.Debug($"Initializing update sources using configProvider {configProvider}");
            this.configProvider = configProvider;
            knownSources
                .Connect()
                .Bind(out var known)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            KnownSources = known;
            
            GetKnownSources().ForEach(x => knownSources.AddOrUpdate(x));
            configProvider
                .ListenTo(x => x.UpdateSource)
                .SubscribeSafe(configSource =>
                {
                    if (!configSource.IsValid)
                    {
                        Log.Warn($"UpdateSource loaded from configuration is not valid or not set: {configSource}");
                    }
                    else
                    {
                        Log.Debug($"UpdateSource in config has changed to {configSource}");
                    }
                    
                    // remapping config source to known source, some details may differ
                    var knownSource = knownSources.Lookup(configSource.Uri ?? string.Empty);
                    if (!knownSource.HasValue)
                    {
                        Log.Warn($"UpdateSource that was loaded from config is now known: {configSource}, resetting to first of known sources:\r\n\t{KnownSources.DumpToTable()}");
                        knownSource = Optional<UpdateSourceInfo>.Create(KnownSources.FirstOrDefault());
                    }

                    Log.Debug($"Setting UpdateSource to {configSource}");
                    UpdateSource = knownSource.Value;
                }, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.UpdateSource)
                .DistinctUntilChanged()
                .Where(x => x != default)
                .Where(x => configProvider.ActualConfig.UpdateSource != x)
                .SubscribeSafe(x =>
                {
                    Log.Debug($"Updating UpdateSource {configProvider.ActualConfig.UpdateSource} => {x}");
                    var config = configProvider.ActualConfig with
                    {
                        UpdateSource = x
                    };
                    configProvider.Save(config);
                }, Log.HandleUiException)
                .AddTo(Anchors);

            knownSources
                .Connect()
                .OnItemUpdated((curr, prev) =>
                {
                    Log.Debug($"UpdateSource updated(duh): {prev} => {curr}");
                    if (curr.Uri != updateSource.Uri)
                    {
                        return;
                    }

                    Log.Debug($"Replacing current update source: {updateSource} => {curr}");
                    UpdateSource = curr;
                })
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
        }

        public UpdateSourceInfo UpdateSource
        {
            get => updateSource;
            set => this.RaiseAndSetIfChanged(ref updateSource, value);
        }

        public ReadOnlyObservableCollection<UpdateSourceInfo> KnownSources { get; }
        
        public void AddSource(UpdateSourceInfo sourceInfo)
        {
            var existingSource = knownSources.Lookup(sourceInfo.Uri);
            if (existingSource.HasValue)
            {
                Log.Debug($"Updating source {existingSource.Value} => {sourceInfo}");
            }
            knownSources.AddOrUpdate(sourceInfo);
        }

        private IEnumerable<UpdateSourceInfo> GetKnownSources()
        {
            Log.Debug($"Fetching config of type {typeof(UpdateSettingsConfig)} from {configProvider}");
            var actualConfig = configProvider.ActualConfig.UpdateSource;
            Log.Debug($"Configured update source is {actualConfig} (isValid: {actualConfig.IsValid})");
            if (actualConfig.IsValid)
            {
                yield return actualConfig;
            }
        }
    }
}