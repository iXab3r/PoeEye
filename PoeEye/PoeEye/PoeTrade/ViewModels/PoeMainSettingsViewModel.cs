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
        private bool whisperNotificationsEnabled;
        
        private readonly PoeEyeMainConfig temporaryConfig = new PoeEyeMainConfig();

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
            config.TransferPropertiesTo(temporaryConfig);

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
            temporaryConfig.ClipboardMonitoringEnabled = ClipboardMonitoringEnabled;
            temporaryConfig.WhisperNotificationsEnabled = WhisperNotificationsEnabled;
            temporaryConfig.CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1.CurrencyType, x => x.Item2);
            
            var result = new PoeEyeMainConfig();
            temporaryConfig.TransferPropertiesTo(result);
            
            return result;
        }
    }
}