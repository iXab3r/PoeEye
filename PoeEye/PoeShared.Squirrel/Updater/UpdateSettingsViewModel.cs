using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Squirrel.Updater;

[UsedImplicitly]
internal sealed class UpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<UpdateSettingsConfig>
{
    private static readonly IFluentLog Log = typeof(UpdateSettingsViewModel).PrepareLogger();

    private readonly IUpdateSourceProvider updateSourceProvider;
    private readonly IConfigProvider<UpdateSettingsConfig> configProvider;

    public UpdateSettingsViewModel(
        IUpdateSourceProvider updateSourceProvider,
        IConfigProvider<UpdateSettingsConfig> configProvider)
    {
        this.updateSourceProvider = updateSourceProvider;
        this.configProvider = configProvider;

        KnownSources = updateSourceProvider.KnownSources;
    }

    public string ModuleName { get; } = "Update Settings";

    public bool CheckForUpdates { get; set; }

    public UpdateSourceInfo UpdateSource { get; set; }

    public bool IgnoreDeltaUpdates { get; set; }
        
    public ReadOnlyObservableCollection<UpdateSourceInfo> KnownSources { get; }
        
    public Task Load(UpdateSettingsConfig config)
    {
        CheckForUpdates = config.AutoUpdateTimeout > TimeSpan.Zero;
        UpdateSource = config.UpdateSource;
        IgnoreDeltaUpdates = config.IgnoreDeltaUpdates;
        return Task.CompletedTask;
    }

    public UpdateSettingsConfig Save()
    {
        var updatedConfig = configProvider.ActualConfig.CloneJson();
        updatedConfig.AutoUpdateTimeout =
            CheckForUpdates ? UpdateSettingsConfig.DefaultAutoUpdateTimeout : TimeSpan.Zero;
        updatedConfig.UpdateSource = UpdateSource;
        updatedConfig.IgnoreDeltaUpdates = IgnoreDeltaUpdates;
        return updatedConfig;
    }
}