using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DynamicData;
using JetBrains.Annotations;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater
{
    [UsedImplicitly]
    internal sealed class UpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<UpdateSettingsConfig>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdateSettingsViewModel));

        private readonly IUpdateSourceProvider updateSourceProvider;
        private readonly IConfigProvider<UpdateSettingsConfig> configProvider;
        private bool checkForUpdates;
        private UpdateSourceInfo updateSource;
        private bool ignoreDeltaUpdates;

        public UpdateSettingsViewModel(
            IUpdateSourceProvider updateSourceProvider,
            IConfigProvider<UpdateSettingsConfig> configProvider)
        {
            this.updateSourceProvider = updateSourceProvider;
            this.configProvider = configProvider;

            KnownSources = updateSourceProvider.KnownSources;
        }

        public string ModuleName { get; } = "Update Settings";

        public bool CheckForUpdates
        {
            get => checkForUpdates;
            set => this.RaiseAndSetIfChanged(ref checkForUpdates, value);
        }

        public UpdateSourceInfo UpdateSource
        {
            get => updateSource;
            set => this.RaiseAndSetIfChanged(ref updateSource, value);
        }

        public bool IgnoreDeltaUpdates
        {
            get => ignoreDeltaUpdates;
            set => RaiseAndSetIfChanged(ref ignoreDeltaUpdates, value);
        }
        
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
            updatedConfig.UpdateSource = updateSource;
            updatedConfig.IgnoreDeltaUpdates = ignoreDeltaUpdates;
            return updatedConfig;
        }
    }
}