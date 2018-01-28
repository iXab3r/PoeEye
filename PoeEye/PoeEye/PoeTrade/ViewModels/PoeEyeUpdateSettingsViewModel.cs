using System;
using PoeEye.Config;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels {
    internal sealed class PoeEyeUpdateSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeUpdateSettingsConfig>
    {
        private bool autoUpdate;
        private PoeEyeUpdateSettingsConfig loadedConfig;

        public bool AutoUpdate
        {
            get { return autoUpdate; }
            set { this.RaiseAndSetIfChanged(ref autoUpdate, value); }
        }
        
        public string ModuleName { get; } = "Update settings";
        
        public void Load(PoeEyeUpdateSettingsConfig config)
        {
            AutoUpdate = config.AutoUpdateTimeout > TimeSpan.Zero;
            loadedConfig = config;
        }
        
        public PoeEyeUpdateSettingsConfig Save()
        {
            var result = new PoeEyeUpdateSettingsConfig();
            loadedConfig.CopyPropertiesTo(result);

            result.AutoUpdateTimeout = AutoUpdate 
                ? PoeEyeUpdateSettingsConfig.DefaultAutoUpdateTimeout 
                : TimeSpan.Zero;
            
            return result;
        }
    }
}