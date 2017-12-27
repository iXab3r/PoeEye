using System;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.Models;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;
using ReactiveUI.Legacy;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeBudConfig>
    {
        private readonly SerialDisposable characterSelectionDisposable = new SerialDisposable();
        private readonly ReactiveUI.Legacy.ReactiveCommand<object> loginCommand;
        private readonly IUiOverlaysProvider overlaysProvider;
        private readonly IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory;

        private readonly PoeBudConfig resultingConfig = new PoeBudConfig();

        private bool hideXpBar;

        private string hotkey;

        private bool isBusy;

        private bool isEnabled;

        private Exception loginException;

        private IReactiveList<TabSelectionViewModel> stashesList;
        private UiOverlayInfo selectedUiOverlay;
        private string sessionId;
        private string username;
        private string selectedLeague;
        private string[] leaguesList;

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

            loginCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            loginCommand
                .Do(x => IsBusy = true)
                .ObserveOn(bgScheduler)
                .Do(LoginCommandExecuted)
                .ObserveOn(uiScheduler)
                .Do(x => IsBusy = false)
                .Subscribe();

            Observable
                .Merge(
                    this.WhenAnyValue(x => x.LeaguesList).ToUnit(),
                    this.WhenAnyValue(x => x.Username).ToUnit(),
                    this.WhenAnyValue(x => x.SelectedLeague).ToUnit())
                .Subscribe(() => this.RaisePropertyChanged(nameof(CanSave)))
                .AddTo(Anchors);

            HotkeysList = KeyGestureExtensions.GetHotkeyList();
            Hotkey = HotkeysList.First();
        }

        public ICommand LoginCommand => loginCommand;

        public string Hotkey
        {
            get { return hotkey; }
            set { this.RaiseAndSetIfChanged(ref hotkey, value); }
        }

        public string[] HotkeysList { get; set; }

        public string[] LeaguesList
        {
            get { return leaguesList; }
            private set { this.RaiseAndSetIfChanged(ref leaguesList, value); }
        }

        public bool HideXpBar
        {
            get { return hideXpBar; }
            set { this.RaiseAndSetIfChanged(ref hideXpBar, value); }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { this.RaiseAndSetIfChanged(ref isEnabled, value); }
        }

        public string SelectedLeague
        {
            get { return selectedLeague; }
            set { this.RaiseAndSetIfChanged(ref selectedLeague, value); }
        }

        public IReactiveList<TabSelectionViewModel> StashesList
        {
            get { return stashesList; }
            set { this.RaiseAndSetIfChanged(ref stashesList, value); }
        }

        public UiOverlayInfo SelectedUiOverlay
        {
            get { return selectedUiOverlay; }
            set { this.RaiseAndSetIfChanged(ref selectedUiOverlay, value); }
        }

        public IReactiveList<UiOverlayInfo> OverlaysList => overlaysProvider.OverlaysList;

        public string Username
        {
            get { return username; }
            set { this.RaiseAndSetIfChanged(ref username, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public string SessionId
        {
            get { return sessionId; }
            set { this.RaiseAndSetIfChanged(ref sessionId, value); }
        }

        public Exception LoginException
        {
            get { return loginException; }
            set { this.RaiseAndSetIfChanged(ref loginException, value); }
        }

        public bool CanSave => !string.IsNullOrEmpty(username)
                               && LeaguesList != null
                               && SelectedLeague != null
                               && StashesList != null && StashesList.Any(x => x.IsSelected);

        public string ModuleName => "Poe Buddy";

        public void Load(PoeBudConfig config)
        {
            config.TransferPropertiesTo(resultingConfig);

            Username = config.LoginEmail;
            LeaguesList = null;   
            SelectedLeague = null;
            StashesList = null;
            Hotkey = config.GetChaosSetHotkey;
            HideXpBar = config.HideXpBar;
            IsEnabled = config.IsEnabled;
            SelectedUiOverlay = OverlaysList.FirstOrDefault(x => x.Name == config.UiOverlayName);
            if (SelectedUiOverlay.Name == null)
            {
                SelectedUiOverlay = UiOverlayInfo.Empty;
            }
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
            resultingConfig.UiOverlayName = selectedUiOverlay.Name;

            resultingConfig.GetChaosSetHotkey = hotkey;
            resultingConfig.HideXpBar = hideXpBar;
            resultingConfig.IsEnabled = isEnabled;

            if (stashesList != null)
            {
                var selectedTabs = stashesList
                    .Where(x => x.IsSelected)
                    .Select(x => x.Tab.Name)
                    .ToArray();

                resultingConfig.StashesToProcess = selectedTabs;
            }

            return resultingConfig;
        }

        private void LoginCommandExecuted(object arg)
        {
            var passwordBox = arg as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }

            LoginException = null;
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new UnauthorizedAccessException("Username (e-mail) is not set");
                }

                if (string.IsNullOrEmpty(passwordBox.Password))
                {
                    throw new UnauthorizedAccessException("Password is not set");
                }

                var poeClient = poeClientFactory.Create(new NetworkCredential(username, passwordBox.Password), false);
                poeClient.Authenticate();
                if (poeClient.IsAuthenticated)
                {
                    SessionId = poeClient.SessionId;
                }
                var characters = poeClient.GetCharacters();

                var leagueAnchors = new CompositeDisposable();
                characterSelectionDisposable.Disposable = leagueAnchors;

                var stashes = characters
                    .Select(x => x.League)
                    .Where(league => !string.IsNullOrEmpty(league))
                    .Distinct()
                    .Select(league => new { League = league, Stash = TryGetStash(poeClient, league)})
                    .Where(x => x.Stash != null && x.League != null)
                    .ToDictionary(
                        x => x.League,
                        x =>
                            new ReactiveList<TabSelectionViewModel>(
                                x.Stash.Tabs.Select(tab => new TabSelectionViewModel(tab))));

                foreach (var kvp in stashes)
                {
                    kvp.Value.ChangeTrackingEnabled = true;
                    leagueAnchors.Add(
                        kvp.Value.ItemChanged.Subscribe(() => this.RaisePropertyChanged(nameof(CanSave)))
                            .AddTo(leagueAnchors));
                }

                var leagueList = stashes.Keys.ToArray();
                LeaguesList = leagueList;

                this
                    .WhenAnyValue(x => x.SelectedLeague)
                    .Subscribe(
                        league => StashesList = league == null || !stashes.ContainsKey(league)
                            ? null
                            : stashes[league]).AddTo(leagueAnchors)
                    .AddTo(leagueAnchors);

                if (selectedLeague != null)
                {
                    var newSelectedLeague = leagueList.FirstOrDefault(x => x == selectedLeague);
                    SelectedLeague = newSelectedLeague;
                }

                if (selectedLeague == null)
                {
                    SelectedLeague = leagueList.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LoginException = ex;
            }
        }

        private IStash TryGetStash(IPoeStashClient client, string league)
        {
            try
            {
                return client.GetStash(0, league);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
                return null;
            }
        }
    }
}
