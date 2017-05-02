using System.Linq;
using PoeEye.Config;
using PoeEye.Utilities;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.UI.ViewModels;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeMainSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeMainConfig>
    {
        private readonly ReactiveList<EditableTuple<PoePrice, float>> currenciesPriceInChaosOrbs = new ReactiveList<EditableTuple<PoePrice, float>>();
        private bool clipboardMonitoringEnabled;
        private bool isOpen;
        private PoeEyeMainConfig loadedConfig;
        private PoeEyeTabConfig[] tabConfigs;
        private bool whisperNotificationsEnabled;

        public PoeMainSettingsViewModel()
        {
            if (AppArguments.Instance.IsDebugMode)
            {
                CurrencyTest = new CurrencyTestViewModel();
            }
        }

        public bool IsOpen
        {
            get => isOpen;
            set => this.RaiseAndSetIfChanged(ref isOpen, value);
        }

        public IReactiveList<EditableTuple<PoePrice, float>> CurrenciesPriceInChaosOrbs => currenciesPriceInChaosOrbs;

        public bool WhisperNotificationsEnabled
        {
            get => whisperNotificationsEnabled;
            set => this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value);
        }

        public bool ClipboardMonitoringEnabled
        {
            get => clipboardMonitoringEnabled;
            set => this.RaiseAndSetIfChanged(ref clipboardMonitoringEnabled, value);
        }

        public CurrencyTestViewModel CurrencyTest { get; }

        public string ModuleName { get; } = "Main";

        public void Load(PoeEyeMainConfig config)
        {
            loadedConfig = config;

            tabConfigs = config.TabConfigs;
            ClipboardMonitoringEnabled = config.ClipboardMonitoringEnabled;
            WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;

            CurrenciesPriceInChaosOrbs.Clear();
            config
                .CurrenciesPriceInChaos
                .EmptyIfNull()
                .Select(
                    x => new EditableTuple<PoePrice, float>
                    {
                        Item1 = StringToPoePriceConverter.Instance.Convert(x.Key),
                        Item2 = x.Value
                    })
                .ForEach(CurrenciesPriceInChaosOrbs.Add);
        }

        public PoeEyeMainConfig Save()
        {
            loadedConfig.TabConfigs = tabConfigs;
            loadedConfig.ClipboardMonitoringEnabled = ClipboardMonitoringEnabled;
            loadedConfig.WhisperNotificationsEnabled = WhisperNotificationsEnabled;
            loadedConfig.CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1.CurrencyType, x => x.Item2);
            return loadedConfig;
        }
    }
}