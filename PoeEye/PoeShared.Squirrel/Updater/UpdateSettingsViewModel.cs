using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows.Data;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Prism;
using ReactiveUI;
using Unity;

namespace PoeShared.Squirrel.Updater;

[UsedImplicitly]
internal sealed class UpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<UpdateSettingsConfig>
{
    private static readonly IFluentLog Log = typeof(UpdateSettingsViewModel).PrepareLogger();

    private readonly IConfigProvider<UpdateSettingsConfig> configProvider;

    public UpdateSettingsViewModel(
        IUpdateSourceProvider updateSourceProvider,
        IConfigProvider<UpdateSettingsConfig> configProvider,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        this.configProvider = configProvider;

        var knownSources = new ObservableCollectionEx<UpdateSourceInfo>();
        updateSourceProvider.WhenAnyValue(x => x.KnownSources)
            .SubscribeSafe(x =>
            {
                knownSources.Clear();
                knownSources.AddRange(x);
            }, Log.HandleUiException)
            .AddTo(Anchors);
        KnownSources = knownSources;
    }

    public string ModuleName { get; } = "Update Settings";

    public bool CheckForUpdates { get; set; }

    public UpdateSourceInfo UpdateSource { get; set; }
    
    public bool IgnoreDeltaUpdates { get; set; }
    
    public bool AutomaticallyDownloadUpdates { get; set; }
    
    public IReadOnlyObservableCollection<UpdateSourceInfo> KnownSources { get; }
        
    public Task Load(UpdateSettingsConfig config)
    {
        CheckForUpdates = config.AutoUpdateTimeout > TimeSpan.Zero;
        UpdateSource = KnownSources.FirstOrDefault(x => x.Id == config.UpdateSourceId);
        IgnoreDeltaUpdates = config.IgnoreDeltaUpdates;
        AutomaticallyDownloadUpdates = config.AutomaticallyDownloadUpdates;
        return Task.CompletedTask;
    }

    public UpdateSettingsConfig Save()
    {
        var updatedConfig = configProvider.ActualConfig with
        {
            AutoUpdateTimeout = CheckForUpdates ? UpdateSettingsConfig.DefaultAutoUpdateTimeout : TimeSpan.Zero,
            UpdateSourceId = UpdateSource?.Id,
            IgnoreDeltaUpdates = IgnoreDeltaUpdates,
            AutomaticallyDownloadUpdates = AutomaticallyDownloadUpdates
        };
        return updatedConfig;
    }
    
    private sealed class CollectionViewWithComparer : CollectionView
    {
        public CollectionViewWithComparer(
            [NotNull] IEnumerable collection,
            IComparer comparer) : base(collection)
        {
            Comparer = comparer;
        }

        public override IComparer Comparer { get; }
    }
}