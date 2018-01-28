using System;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicData.Annotations;
using Guards;
using Microsoft.Practices.Unity;
using PoeEye.Config;
using PoeEye.Utilities;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using ReactiveUI;
using Squirrel;

namespace PoeEye.PoeTrade.ViewModels {
    internal sealed class PoeEyeUpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeUpdateSettingsConfig>
    {
        private bool autoUpdate;
        private PoeEyeUpdateSettingsConfig loadedConfig;
        private UpdateSourceInfo updateSource;
        private string username;
        private string updateSourcePatchNotes;
        private PasswordBox passwordBox;

        public PoeEyeUpdateSettingsViewModel(
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

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
            get { return autoUpdate; }
            set { this.RaiseAndSetIfChanged(ref autoUpdate, value); }
        }

        public UpdateSourceInfo UpdateSource
        {
            get { return updateSource; }
            set { this.RaiseAndSetIfChanged(ref updateSource, value); }
        }

        public string Username
        {
            get { return username; }
            set { this.RaiseAndSetIfChanged(ref username, value); }
        }

        public PasswordBox PasswordBox
        {
            get { return passwordBox; }
            set { this.RaiseAndSetIfChanged(ref passwordBox, value); }
        }
        
        public UpdateSourceInfo[] KnownUpdateSources { get; }
        
        public ICommand TestConnectionCommand { get; }

        public string UpdateSourcePatchNotes
        {
            get { return updateSourcePatchNotes; }
            set { this.RaiseAndSetIfChanged(ref updateSourcePatchNotes, value); }
        }
        
        public string ModuleName { get; } = "Update settings";
        
        public void Load(PoeEyeUpdateSettingsConfig config)
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
                updateSource.Password = PasswordBox.Password;
            }
            result.UpdateSource = updateSource;
            
            return result;
        }
        
        private async Task TestConnectionCommandExecuted(NetworkCredential credentials)
        {
            UpdateSourcePatchNotes = null;
            var fakeUpdateDirectory = Path.Combine(Path.GetTempPath(), "PoeEye");
            var downloader = new BasicAuthFileDownloader(credentials);
            using (var updateManager = new UpdateManager(UpdateSource.Uri, "PoeEye", fakeUpdateDirectory, downloader))
            {
                Log.Instance.Debug($"[TestConnection] Checking for updates...");
                await Task.Delay(UiConstants.ArtificialShortDelay);
                var updateInfo = await updateManager.CheckForUpdate(true);
                Log.Instance.Debug($"[TestConnection] UpdateInfo:\r\n{updateInfo?.DumpToText()}");

                UpdateSourcePatchNotes = $"Successfully connected to {updateSource.Description}\n{updateInfo?.DumpToText()}";
            }
        }
    }
}