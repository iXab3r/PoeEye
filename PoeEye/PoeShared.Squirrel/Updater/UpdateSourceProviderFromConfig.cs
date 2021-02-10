using System.Collections.Generic;
using System.Collections.ObjectModel;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class UpdateSourceProviderFromConfig : DisposableReactiveObject, IUpdateSourceProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdateSourceProviderFromConfig));

        private readonly IConfigProvider<UpdateSettingsConfig> configProvider;
        private UpdateSourceInfo updateSource;

        public UpdateSourceProviderFromConfig(IConfigProvider<UpdateSettingsConfig> configProvider)
        {
            Log.Debug($"Initializing update sources using configProvider {configProvider}");
            this.configProvider = configProvider;
            GetKnownSources().ForEach(x => KnownSources.Add(x));

            configProvider
                .ListenTo(x => x.UpdateSource)
                .SubscribeSafe(x => UpdateSource = x, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.UpdateSource)
                .DistinctUntilChanged()
                .Where(x => !x.Equals(configProvider.ActualConfig.UpdateSource))
                .SubscribeSafe(
                    x =>
                    {
                        Log.Debug($"Saving UpdateSource into config {configProvider.ActualConfig.UpdateSource} => {x}");
                        var newConfig = configProvider.ActualConfig.CloneJson();
                        newConfig.UpdateSource = x;
                        configProvider.Save(newConfig);
                    }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public UpdateSourceInfo UpdateSource
        {
            get => updateSource;
            set => this.RaiseAndSetIfChanged(ref updateSource, value);
        }

        public HashSet<UpdateSourceInfo> KnownSources { get; } = new HashSet<UpdateSourceInfo>();

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