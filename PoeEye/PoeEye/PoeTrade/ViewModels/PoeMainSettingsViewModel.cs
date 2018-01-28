using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeEye.Config;
using PoeEye.Utilities;
using PoeShared;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.PoeDatabase;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.ViewModels;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeMainSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeMainConfig>
    {
        private readonly IPoeEconomicsSource economicsSource;
        private readonly IConfigProvider<PoeBudConfig> poeBudConfigProvider;
        private readonly ReactiveList<EditableTuple<PoePrice, float>> currenciesPriceInChaosOrbs = new ReactiveList<EditableTuple<PoePrice, float>>();
        private readonly PoeEyeMainConfig temporaryConfig = new PoeEyeMainConfig();

        private bool whisperNotificationsEnabled;
        
        public PoeMainSettingsViewModel(
            [NotNull] IPoeEconomicsSource economicsSource,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(economicsSource, nameof(economicsSource));
            Guard.ArgumentNotNull(poeBudConfigProvider, nameof(poeBudConfigProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.economicsSource = economicsSource;
            this.poeBudConfigProvider = poeBudConfigProvider;
            if (AppArguments.Instance.IsDebugMode)
            {
                CurrencyTest = new CurrencyTestViewModel();
            }

            poeBudConfigProvider.WhenChanged.Subscribe(() => this.RaisePropertyChanged(nameof(LeagueId))).AddTo(Anchors);
            
            GetEconomicsCommand = new CommandWrapper(
                ReactiveCommand.CreateFromTask(RefreshCurrencyList, this.WhenAnyValue(x => x.LeagueId).Select(leagueId => !string.IsNullOrEmpty(leagueId)), outputScheduler: uiScheduler));
        }

        public IReactiveList<EditableTuple<PoePrice, float>> CurrenciesPriceInChaosOrbs => currenciesPriceInChaosOrbs;

        public bool WhisperNotificationsEnabled
        {
            get => whisperNotificationsEnabled;
            set => this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value);
        }

        public CurrencyTestViewModel CurrencyTest { get; }
        
        public CommandWrapper GetEconomicsCommand { get; }

        public string ModuleName { get; } = "Main";

        public string LeagueId => poeBudConfigProvider.ActualConfig.LeagueId;

        public void Load(PoeEyeMainConfig config)
        {
            config.CopyPropertiesTo(temporaryConfig);

            WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;

            CurrenciesPriceInChaosOrbs.Clear();
            config
                .CurrenciesPriceInChaos
                .EmptyIfNull()
                .Select(
                    x => new EditableTuple<PoePrice, float>
                    {
                        Item1 = StringToPoePriceConverter.Instance.Convert(x.Key).SetValue(1),
                        Item2 = x.Value
                    })
                .ForEach(CurrenciesPriceInChaosOrbs.Add);
        }

        public PoeEyeMainConfig Save()
        {
            temporaryConfig.WhisperNotificationsEnabled = WhisperNotificationsEnabled;
            temporaryConfig.CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1.CurrencyType, x => x.Item2);
            
            var result = new PoeEyeMainConfig();
            temporaryConfig.CopyPropertiesTo(result);
            
            return result;
        }

        private async Task RefreshCurrencyList()
        {
            var economics = await Task.Run(() => economicsSource.GetCurrenciesInChaosEquivalent(LeagueId));

            foreach (var economicPrice in economics)
            {
                var editableValue = currenciesPriceInChaosOrbs.FirstOrDefault(x => x.Item1.CurrencyType == economicPrice.CurrencyType);
                if (editableValue == null)
                {
                    continue;
                }
                editableValue.Item2 = economicPrice.Value;
            }
        }
    }
}