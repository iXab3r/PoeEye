using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Controls;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeBud.Models;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;
using Unity.Attributes;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeBudConfig>
    {
        private readonly SerialDisposable characterSelectionDisposable = new SerialDisposable();
        private readonly IUiOverlaysProvider overlaysProvider;
        private readonly IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory;

        private readonly PoeBudConfig resultingConfig = new PoeBudConfig();

        private bool hideXpBar;

        private string hotkey;

        private bool isEnabled;
        private string[] leaguesList;
        private string selectedLeague;

        private UiOverlayInfo selectedUiOverlay;
        private string sessionId;
        private string username;

        public PoeBudSettingsViewModel(
            [NotNull] IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory,
            [NotNull] IUiOverlaysProvider overlaysProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(overlaysProvider, nameof(overlaysProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(poeClientFactory, nameof(poeClientFactory));

            this.poeClientFactory = poeClientFactory;
            this.overlaysProvider = overlaysProvider;

            LoginCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask<object>(x => LoginCommandExecuted(x), null, uiScheduler));

            HotkeysList = KeyGestureExtensions.GetHotkeyList();
            Hotkey = HotkeysList.First();
        }

        public CommandWrapper LoginCommand { get; }

        public string Hotkey
        {
            get => hotkey;
            set => this.RaiseAndSetIfChanged(ref hotkey, value);
        }

        public string[] HotkeysList { get; }

        public string[] LeaguesList
        {
            get => leaguesList;
            private set => this.RaiseAndSetIfChanged(ref leaguesList, value);
        }

        public string SelectedLeague
        {
            get => selectedLeague;
            set => this.RaiseAndSetIfChanged(ref selectedLeague, value);
        }

        public IReactiveList<TabSelectionViewModel> StashesList { get; } = new ReactiveList<TabSelectionViewModel> {ChangeTrackingEnabled = true};

        public string Username
        {
            get => username;
            set => this.RaiseAndSetIfChanged(ref username, value);
        }

        public string SessionId
        {
            get => sessionId;
            set => this.RaiseAndSetIfChanged(ref sessionId, value);
        }

        public string ModuleName => "Poe Buddy";

        public async Task Load(PoeBudConfig config)
        {
            config.CopyPropertiesTo(resultingConfig);

            Username = config.LoginEmail;

            LeaguesList = string.IsNullOrEmpty(config.LeagueId) ? null : new[] {config.LeagueId};
            SelectedLeague = config.LeagueId;

            StashesList.Clear();
            if (config.StashesToProcess != null && config.StashesToProcess.Any())
            {
                config.StashesToProcess
                      .Select(x => new TabSelectionViewModel(x) {IsSelected = true})
                      .ForEach(StashesList.Add);
            }

            Hotkey = config.GetChaosSetHotkey;
        }

        public PoeBudConfig Save()
        {
            if (!string.IsNullOrEmpty(Username))
            {
                resultingConfig.LoginEmail = Username;
            }

            if (!string.IsNullOrEmpty(SessionId))
            {
                resultingConfig.SessionId = SessionId;
            }

            if (SelectedLeague != null)
            {
                resultingConfig.LeagueId = SelectedLeague;
            }

            resultingConfig.GetChaosSetHotkey = hotkey;
            resultingConfig.IsEnabled = isEnabled;

            var selectedTabs = StashesList
                               .Where(x => x.IsSelected)
                               .Select(x => x.Name)
                               .ToArray();

            resultingConfig.StashesToProcess = selectedTabs;

            var result = new PoeBudConfig();
            resultingConfig.CopyPropertiesTo(result);

            return result;
        }

        private async Task LoginCommandExecuted(object arg)
        {
            var passwordBox = arg as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new UnauthorizedAccessException("Username (e-mail) is not set");
            }

            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                throw new UnauthorizedAccessException("Password is not set");
            }

            var poeClient = poeClientFactory.Create(new NetworkCredential(username, passwordBox.Password), false);
            Log.Instance.Debug($"[PoeBudSettings.LoginCommand] Authenticating as {username}...");
            await poeClient.AuthenticateAsync();
            if (poeClient.IsAuthenticated)
            {
                SessionId = poeClient.SessionId;
            }

            Log.Instance.Debug($"[PoeBudSettings.LoginCommand] SessionId: {poeClient.SessionId}");

            Log.Instance.Debug($"[PoeBudSettings.LoginCommand] Requesting characters list...");
            var characters = await poeClient.GetCharactersAsync();

            var leagueAnchors = new CompositeDisposable();
            characterSelectionDisposable.Disposable = leagueAnchors;

            var leagues = characters
                          .Select(x => x.League)
                          .Where(league => !string.IsNullOrEmpty(league))
                          .Distinct()
                          .ToArray();

            Log.Instance.Debug(
                $"[PoeBudSettings.LoginCommand] Response received, characters list: \n\t{characters.DumpToTable()}\nLeagues list: \n\t{leagues.DumpToTable()}");

            Log.Instance.Debug($"[PoeBudSettings.LoginCommand] Requesting stashes list...");
            var stashes = await TryGetStash(poeClient, leagues);

            var leagueStashViewModels = stashes
                                        .Where(x => x.Stash != null && !string.IsNullOrWhiteSpace(x.LeagueId))
                                        .ToDictionary(
                                            x => x.LeagueId,
                                            x => x.Stash.Tabs.Where(tab => !string.IsNullOrWhiteSpace(tab.Name))
                                                  .Select(tab => new TabSelectionViewModel(tab.Name)).ToArray());

            var leagueList = leagueStashViewModels.Keys.ToArray();
            LeaguesList = leagueList;

            this
                .WhenAnyValue(x => x.SelectedLeague)
                .Subscribe(
                    league =>
                    {
                        StashesList.Clear();
                        if (league != null)
                        {
                            leagueStashViewModels[league].ForEach(StashesList.Add);
                        }
                    })
                .AddTo(leagueAnchors);

            if (SelectedLeague != null)
            {
                var newSelectedLeague = leagueList.FirstOrDefault(x => x == selectedLeague);
                SelectedLeague = newSelectedLeague;
            }

            if (SelectedLeague == null)
            {
                SelectedLeague = leagueList.FirstOrDefault();
            }
        }

        private async Task<LeagueStashes[]> TryGetStash(IPoeStashClient client, IEnumerable<string> leagues)
        {
            var result = new List<LeagueStashes>();
            foreach (var league in leagues)
            {
                var stash = await TryGetStash(client, league);
                if (stash == null)
                {
                    continue;
                }

                result.Add(new LeagueStashes {LeagueId = league, Stash = stash});
            }

            return result.ToArray();
        }

        private Task<IStash> TryGetStash(IPoeStashClient client, string league)
        {
            try
            {
                Log.Instance.Debug($"[PoeBudSettings.LoginCommand] Requesting stash for league {league}...");
                return client.GetStashAsync(0, league);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
                return Task.FromResult(default(IStash));
            }
        }

        private struct LeagueStashes
        {
            public string LeagueId { get; set; }

            public IStash Stash { get; set; }
        }
    }
}