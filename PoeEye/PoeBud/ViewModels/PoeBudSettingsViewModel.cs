﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
using PoeShared.Scaffolding.WPF;
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
        private readonly IUiOverlaysProvider overlaysProvider;
        private readonly IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory;

        private readonly PoeBudConfig resultingConfig = new PoeBudConfig();

        private bool hideXpBar;

        private string hotkey;

        private bool isEnabled;

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
            
            LoginCommand = new CommandWrapper(
                ReactiveCommand.CreateFromTask<object>(x => LoginCommandExecuted(x), null, uiScheduler));

            HotkeysList = KeyGestureExtensions.GetHotkeyList();
            Hotkey = HotkeysList.First();
        }

        public CommandWrapper LoginCommand { get; }

        public string Hotkey
        {
            get { return hotkey; }
            set { this.RaiseAndSetIfChanged(ref hotkey, value); }
        }

        public string[] HotkeysList { get; }

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

        public IReactiveList<TabSelectionViewModel> StashesList { get; } = new ReactiveList<TabSelectionViewModel>() { ChangeTrackingEnabled = true };

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

        public string SessionId
        {
            get { return sessionId; }
            set { this.RaiseAndSetIfChanged(ref sessionId, value); }
        }

        public string ModuleName => "Poe Buddy";

        public void Load(PoeBudConfig config)
        {
            config.CopyPropertiesTo(resultingConfig);

            Username = config.LoginEmail;
            
            LeaguesList = string.IsNullOrEmpty(config.LeagueId) ? null : new[] { config.LeagueId };   
            SelectedLeague = config.LeagueId;
            
            StashesList.Clear();
            if (config.StashesToProcess != null && config.StashesToProcess.Any())
            {
                config.StashesToProcess
                    .Select(x => new TabSelectionViewModel(x) { IsSelected = true })
                    .ForEach(StashesList.Add);
            }
            
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
                    .Select(x => x.Name)
                    .ToArray();

                resultingConfig.StashesToProcess = selectedTabs;
            }

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
            await poeClient.AuthenticateAsync();
            if (poeClient.IsAuthenticated)
            {
                SessionId = poeClient.SessionId;
            }
            var characters = await poeClient.GetCharactersAsync();

            var leagueAnchors = new CompositeDisposable();
            characterSelectionDisposable.Disposable = leagueAnchors;

            var leagues = characters
                .Select(x => x.League)
                .Where(league => !string.IsNullOrEmpty(league))
                .Distinct()
                .ToArray();
            var stashes = await TryGetStash(client: poeClient, leagues: leagues);

            var leagueStashViewModels = stashes
                .Where(x => x.Stash != null && x.LeagueId != null)
                .ToDictionary(
                    x => x.LeagueId,
                    x => x.Stash.Tabs.Select(tab => new TabSelectionViewModel(tab.Name)).ToArray());

            var leagueList = leagueStashViewModels.Keys.ToArray();
            LeaguesList = leagueList;

            this
                .WhenAnyValue(x => x.SelectedLeague)
                .Subscribe(
                    league =>
                    {
                        StashesList.Clear();
                        leagueStashViewModels[league].ForEach(StashesList.Add);
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
                result.Add(new LeagueStashes(){ LeagueId = league, Stash = stash });
            }
            return result.ToArray();
        }

        private Task<IStash> TryGetStash(IPoeStashClient client, string league)
        {
            try
            {
                return client.GetStashAsync(stashIdx: 0, league: league);
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
