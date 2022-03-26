using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater;

internal sealed class UpdateSourceProviderFromConfig : DisposableReactiveObject, IUpdateSourceProvider
{
    private static readonly IFluentLog Log = typeof(UpdateSourceProviderFromConfig).PrepareLogger();

    public UpdateSourceProviderFromConfig(IConfigProvider<UpdateSettingsConfig> configProvider)
    {
        Log.Debug(() => $"Initializing update sources using configProvider {configProvider}");

        configProvider.ListenTo(x => x.UpdateSource)
            .SubscribeSafe(x => UpdateSource = x, Log.HandleUiException)
            .AddTo(Anchors);
        this.WhenAnyValue(x => x.UpdateSource)
            .SubscribeSafe(x =>
            {
                if (configProvider.ActualConfig.UpdateSource == x)
                {
                    return;
                }
                
                Log.Debug(() => $"Saving update source to config: {x}");
                var config = configProvider.ActualConfig with
                {
                    UpdateSource = x
                };
                configProvider.Save(config);
            }, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.KnownSources)
            .CombineLatest(this.WhenAnyValue(x => x.UpdateSource), (items, selected) => new {items, selected})
            .SubscribeSafe(x =>
            {
                if (x.items == null || x.items.IsEmpty())
                {
                    return;
                }

                if (x.selected == default)
                {
                    return;
                }

                if (x.items.Contains(x.selected) && x.selected.IsValid)
                {
                    return;
                }

                var defaultSource = x.items[0];
                Log.Debug(() => $"Defaulting update source to {defaultSource}");
                UpdateSource = defaultSource;
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public UpdateSourceInfo UpdateSource { get; set; }
    
    public IReadOnlyList<UpdateSourceInfo> KnownSources { get; set; }

    private static string ToKey(UpdateSourceInfo updateSourceInfo)
    {
        return updateSourceInfo.Id ?? string.Empty;
    }
}