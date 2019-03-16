using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DynamicData.Annotations;
using Guards;
using PoeEye.Config;
using PoeEye.Updates;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.Settings.ViewModels
{
    internal sealed class PoeEyeUpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeUpdateSettingsConfig>
    {
        private readonly IApplicationUpdaterModel updaterModel;
        private bool autoUpdate;
        private PoeEyeUpdateSettingsConfig loadedConfig;
        private PasswordBox passwordBox;
        private UpdateSourceInfo updateSource;
        private string username;

        public PoeEyeUpdateSettingsViewModel(
            [NotNull] IApplicationUpdaterModel updaterModel,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(updaterModel, nameof(updaterModel));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            this.updaterModel = updaterModel;

            KnownUpdateSources = PoeEyeUpdateSettingsConfig.WellKnownUpdateSources;

            TestConnectionCommand = CommandWrapper.Create(
                () => TestConnectionCommandExecuted(new NetworkCredential(Username, PasswordBox?.Password)));

            this.WhenAnyValue(x => x.PasswordBox)
                .Where(box => box != null && loadedConfig != null)
                .ObserveOn(uiScheduler)
                .Subscribe(x => x.Password = loadedConfig.UpdateSource.Password)
                .AddTo(Anchors);
        }

        public bool AutoUpdate
        {
            get => autoUpdate;
            set => this.RaiseAndSetIfChanged(ref autoUpdate, value);
        }

        public UpdateSourceInfo UpdateSource
        {
            get => updateSource;
            set => this.RaiseAndSetIfChanged(ref updateSource, value);
        }

        public string Username
        {
            get => username;
            set => this.RaiseAndSetIfChanged(ref username, value);
        }

        public PasswordBox PasswordBox
        {
            get => passwordBox;
            set => this.RaiseAndSetIfChanged(ref passwordBox, value);
        }

        public UpdateSourceInfo[] KnownUpdateSources { get; }

        public CommandWrapper TestConnectionCommand { get; }

        public string ModuleName { get; } = "Update settings";

        public async Task Load(PoeEyeUpdateSettingsConfig config)
        {
            loadedConfig = config;

            AutoUpdate = config.AutoUpdateTimeout > TimeSpan.Zero;
            UpdateSource = config.UpdateSource;
            Username = config.UpdateSource.Username;
            if (PasswordBox != null)
            {
                PasswordBox.Password = config.UpdateSource.Password;
            }
        }

        public PoeEyeUpdateSettingsConfig Save()
        {
            var result = new PoeEyeUpdateSettingsConfig();
            loadedConfig.CopyPropertiesTo(result);

            result.AutoUpdateTimeout = AutoUpdate
                ? PoeEyeUpdateSettingsConfig.DefaultAutoUpdateTimeout
                : TimeSpan.Zero;

            if (updateSource.RequiresAuthentication)
            {
                updateSource.Username = Username;
                updateSource.Password = PasswordBox?.Password;
            }

            result.UpdateSource = updateSource;

            return result;
        }

        private async Task TestConnectionCommandExecuted(NetworkCredential credentials)
        {
            TestConnectionCommand.Description = null;
            updaterModel.UpdateSource = UpdateSource;

            var updateInfo = await updaterModel.CheckForUpdates();
            TestConnectionCommand.Description = $"Successfully connected to {updateSource.Description}\n{updateInfo?.DumpToText()}";
        }
    }
}