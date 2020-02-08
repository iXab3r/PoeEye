using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater
{
    [UsedImplicitly]
    internal sealed class UpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<UpdateSettingsConfig>
    {
        private readonly IConfigProvider<UpdateSettingsConfig> configProvider;
        private bool checkForUpdates;
        private UpdateSourceInfo updateSource;

        public UpdateSettingsViewModel(
            [NotNull] IConfigProvider<UpdateSettingsConfig> configProvider)
        {
            this.configProvider = configProvider;
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
        
        public Task Load(UpdateSettingsConfig config)
        {
            CheckForUpdates = config.AutoUpdateTimeout > TimeSpan.Zero;
            UpdateSource = config.UpdateSource;
            return Task.CompletedTask;
        }

        public UpdateSettingsConfig Save()
        {
            var updatedConfig = configProvider.ActualConfig.CloneJson();
            updatedConfig.AutoUpdateTimeout =
                CheckForUpdates ? UpdateSettingsConfig.DefaultAutoUpdateTimeout : TimeSpan.Zero;
            updatedConfig.UpdateSource = UpdateSource;
            return updatedConfig;
        }
    }
}