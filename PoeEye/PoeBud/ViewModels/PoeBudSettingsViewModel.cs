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
using NuGet;
using PoeBud.Config;
using PoeBud.Models;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeBudConfig>
    {
        private readonly SerialDisposable characterSelectionDisposable = new SerialDisposable();
        private readonly ReactiveCommand<object> loginCommand;
        [NotNull] private readonly IUiOverlaysProvider overlaysProvider;
        private readonly IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory;

        private readonly PoeBudConfig resultingConfig = new PoeBudConfig();

        private ICharacter[] charactersList;

        private bool hideXpBar;

        private string hotkey;

        private bool isBusy;

        private bool isEnabled;

        private Exception loginException;

        private ICharacter selectedCharacter;

        private IReactiveList<TabSelectionViewModel> selectedCharacterStash;

        private UiOverlayInfo selectedUiOverlay;

        private string sessionId;

        private string username;

        public PoeBudSettingsViewModel(
            [NotNull] IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory,
            [NotNull] IUiOverlaysProvider overlaysProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => overlaysProvider);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => poeClientFactory);

            this.poeClientFactory = poeClientFactory;
            this.overlaysProvider = overlaysProvider;

            loginCommand = ReactiveCommand.Create();
            loginCommand
                .Do(x => IsBusy = true)
                .ObserveOn(bgScheduler)
                .Do(LoginCommandExecuted)
                .ObserveOn(uiScheduler)
                .Do(x => IsBusy = false)
                .Subscribe();

            Observable
                .Merge(
                    this.WhenAnyValue(x => x.CharactersList).ToUnit(),
                    this.WhenAnyValue(x => x.Username).ToUnit(),
                    this.WhenAnyValue(x => x.SelectedCharacter).ToUnit())
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

        public ICharacter[] CharactersList
        {
            get { return charactersList; }
            set { this.RaiseAndSetIfChanged(ref charactersList, value); }
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

        public ICharacter SelectedCharacter
        {
            get { return selectedCharacter; }
            set { this.RaiseAndSetIfChanged(ref selectedCharacter, value); }
        }

        public IReactiveList<TabSelectionViewModel> SelectedCharacterStash
        {
            get { return selectedCharacterStash; }
            set { this.RaiseAndSetIfChanged(ref selectedCharacterStash, value); }
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
                               && CharactersList != null
                               && SelectedCharacter != null
                               && SelectedCharacterStash != null && SelectedCharacterStash.Any(x => x.IsSelected);

        public string ModuleName => "Poe Buddy";

        public void Load(PoeBudConfig config)
        {
            config.TransferPropertiesTo(resultingConfig);

            Username = config.LoginEmail;
            SelectedCharacter = null;   
            CharactersList = null;
            SelectedCharacterStash = null;
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
            if (SelectedCharacter != null)
            {
                resultingConfig.CharacterName = SelectedCharacter.Name;
            }
            resultingConfig.UiOverlayName = selectedUiOverlay.Name;

            resultingConfig.GetChaosSetHotkey = hotkey;
            resultingConfig.HideXpBar = hideXpBar;
            resultingConfig.IsEnabled = isEnabled;

            if (selectedCharacterStash != null)
            {
                var selectedTabs = selectedCharacterStash
                    .Where(x => x.IsSelected)
                    .Select(x => x.Tab.Idx)
                    .ToArray();

                resultingConfig.StashesToProcess.Clear();
                resultingConfig.StashesToProcess.AddRange(selectedTabs);
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

                var characterDisposable = new CompositeDisposable();
                characterSelectionDisposable.Disposable = characterDisposable;

                var stashes = characters
                    .Select(x => new {Character = x, Stash = TryGetStash(poeClient, x.League)})
                    .Where(x => x.Stash != null && x.Character != null)
                    .ToDictionary(
                        x => x.Character,
                        x =>
                            new ReactiveList<TabSelectionViewModel>(
                                x.Stash.Tabs.Select(tab => new TabSelectionViewModel(tab))));

                foreach (var kvp in stashes)
                {
                    kvp.Value.ChangeTrackingEnabled = true;
                    characterDisposable.Add(
                        kvp.Value.ItemChanged.Subscribe(() => this.RaisePropertyChanged(nameof(CanSave)))
                            .AddTo(characterDisposable));
                }

                CharactersList = stashes.Keys.ToArray();

                this
                    .WhenAnyValue(x => x.SelectedCharacter)
                    .Subscribe(
                        character => SelectedCharacterStash = character == null || !stashes.ContainsKey(character)
                            ? null
                            : stashes[character]).AddTo(characterDisposable)
                    .AddTo(characterDisposable);

                if (selectedCharacter != null)
                {
                    var newSelectedCharacter = charactersList.FirstOrDefault(x => x.Name == selectedCharacter.Name);
                    SelectedCharacter = newSelectedCharacter;
                }

                if (selectedCharacter == null)
                {
                    SelectedCharacter = CharactersList.FirstOrDefault();
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