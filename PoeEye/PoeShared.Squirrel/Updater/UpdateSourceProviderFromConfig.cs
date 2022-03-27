using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater;

internal sealed class UpdateSourceProviderFromConfig : DisposableReactiveObject, IUpdateSourceProvider
{
    private static readonly IFluentLog Log = typeof(UpdateSourceProviderFromConfig).PrepareLogger();

    public UpdateSourceProviderFromConfig(IConfigProvider<UpdateSettingsConfig> configProvider)
    {
        Log.Debug(() => $"Initializing update sources using configProvider {configProvider}");

        configProvider.ListenTo(x => x.UpdateSourceId)
            .SubscribeSafe(x => UpdateSourceId = x, Log.HandleUiException)
            .AddTo(Anchors);
        this.WhenAnyValue(x => x.UpdateSourceId)
            .SubscribeSafe(x =>
            {
                if (configProvider.ActualConfig.UpdateSourceId == x)
                {
                    return;
                }
                
                Log.Debug(() => $"Saving update source to config: {x}");
                var config = configProvider.ActualConfig with
                {
                    UpdateSourceId = x
                };
                configProvider.Save(config);
            }, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.KnownSources)
            .CombineLatest(this.WhenAnyValue(x => x.UpdateSourceId), (items, selectedId) => new {items, selectedId})
            .SubscribeSafe(x =>
            {
                UpdateSource = x.items.EmptyIfNull().FirstOrDefault(y => y.Id == x.selectedId);
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

                if (x.items.Contains(x.selected) && x.selected.IsValid)
                {
                    return;
                }

                var defaultSource = x.items[0];
                Log.Debug(() => $"Defaulting update source to {defaultSource}");
                UpdateSourceId = defaultSource.Id;
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public UpdateSourceInfo UpdateSource { get; private set; }
    
    public string UpdateSourceId { get; set; }
    
    public IReadOnlyList<UpdateSourceInfo> KnownSources { get; set; }
}