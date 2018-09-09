using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
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
using PoeShared.StashApi;
using PoeShared.UI.ViewModels;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeMainSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeEyeMainConfig>
    {
        private readonly IClock clock;
        private readonly ReactiveList<EditableTuple<PoePrice, float>> currenciesPriceInChaosOrbs = new ReactiveList<EditableTuple<PoePrice, float>>();
        private readonly IPoeEconomicsSource economicsSource;
        private readonly IPoeLeagueApiClient leagueApiClient;
        private readonly PoeEyeMainConfig temporaryConfig = new PoeEyeMainConfig();
        private string leagueId;
        private TaskCompletionSource<string[]> leaguesSource;

        private bool whisperNotificationsEnabled;

        public PoeMainSettingsViewModel(
            [NotNull] IClock clock,
            [NotNull] IPoeLeagueApiClient leagueApiClient,
            [NotNull] IPoeEconomicsSource economicsSource,
            [NotNull] IConfigProvider<PoeEyeMainConfig> mainConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(leagueApiClient, nameof(leagueApiClient));
            Guard.ArgumentNotNull(economicsSource, nameof(economicsSource));
            Guard.ArgumentNotNull(mainConfigProvider, nameof(mainConfigProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.clock = clock;
            this.leagueApiClient = leagueApiClient;
            this.economicsSource = economicsSource;
            if (AppArguments.Instance.IsDebugMode)
            {
                CurrencyTest = new CurrencyTestViewModel();
            }

            GetEconomicsCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(RefreshCurrencyList, this.WhenAnyValue(x => x.LeagueId).Select(leagueId => !string.IsNullOrEmpty(leagueId)),
                                               uiScheduler));
        }

        public IReactiveList<EditableTuple<PoePrice, float>> CurrenciesPriceInChaosOrbs => currenciesPriceInChaosOrbs;

        public bool WhisperNotificationsEnabled
        {
            get => whisperNotificationsEnabled;
            set => this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value);
        }

        public CurrencyTestViewModel CurrencyTest { get; }

        public CommandWrapper GetEconomicsCommand { get; }

        public string LeagueId
        {
            get => leagueId;
            set => this.RaiseAndSetIfChanged(ref leagueId, value);
        }

        public IReactiveList<string> LeagueList { get; } = new ReactiveList<string>();

        public string ModuleName { get; } = "Main";

        public async Task Load(PoeEyeMainConfig config)
        {
            config.CopyPropertiesTo(temporaryConfig);

            WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;
            if (LeagueList.IsEmpty)
            {
                var leagues = await leagueApiClient.GetLeaguesAsync();

                leagues
                    .Where(x => x.StartAt <= clock.Now && (x.EndAt >= clock.Now || x.EndAt == DateTime.MinValue))
                    .Where(x => x.Id.IndexOf("SSF", StringComparison.OrdinalIgnoreCase) < 0)
                    .Select(x => x.Id)
                    .ForEach(LeagueList.Add);
            }

            LeagueId = config.LeagueId;

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
            temporaryConfig.LeagueId = LeagueId;

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