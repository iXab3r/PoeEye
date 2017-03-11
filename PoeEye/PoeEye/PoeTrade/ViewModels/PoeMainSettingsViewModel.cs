using System;
using System.Linq;
using PoeEye.Config;
using PoeEye.Utilities;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeMainSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeMainConfig>
    {
        private bool clipboardMonitoringEnabled;

        private EditableTuple<string, float>[] currenciesPriceInChaosOrbs = new EditableTuple<string, float>[0];

        private bool isOpen;

        private bool whisperNotificationsEnabled;

        private PoeEyeTabConfig[] tabConfigs;

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public EditableTuple<string, float>[] CurrenciesPriceInChaosOrbs
        {
            get { return currenciesPriceInChaosOrbs; }
            set { this.RaiseAndSetIfChanged(ref currenciesPriceInChaosOrbs, value); }
        }

        public bool WhisperNotificationsEnabled
        {
            get { return whisperNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value); }
        }

        public bool ClipboardMonitoringEnabled
        {
            get { return clipboardMonitoringEnabled; }
            set { this.RaiseAndSetIfChanged(ref clipboardMonitoringEnabled, value); }
        }

        public string ModuleName { get; } = "Main";

        public void Load(PoeEyeMainConfig config)
        {
            tabConfigs = config.TabConfigs;
            ClipboardMonitoringEnabled = config.ClipboardMonitoringEnabled;
            WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;
            CurrenciesPriceInChaosOrbs = config
                 .CurrenciesPriceInChaos
                 .Select(x => new EditableTuple<string, float> { Item1 = x.Key, Item2 = x.Value })
                 .ToArray();
        }

        public PoeEyeMainConfig Save()
        {
            var config = new PoeEyeMainConfig()
            {
                TabConfigs = tabConfigs,
                ClipboardMonitoringEnabled = ClipboardMonitoringEnabled,
                WhisperNotificationsEnabled = WhisperNotificationsEnabled,
                CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1, x => x.Item2),
            };

            return config;
        }
    }
}