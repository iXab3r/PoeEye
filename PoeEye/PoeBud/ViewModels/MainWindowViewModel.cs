using PoeBud.OfficialApi;
using PoeBud.OfficialApi.DataTypes;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeBud.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using NuGet;

    using Prism;

    using ReactiveUI;

    using Utilities;

    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        private readonly ReactiveCommand<object> closeCommand;
        private readonly ReactiveCommand<object> loginCommand;
        private readonly IPoeBudConfigProvider<IPoeBudConfig> poeBudConfigProvider;
        private readonly IFactory<IPoeClient, NetworkCredential, bool> poeClientFactory;
        private readonly ReactiveCommand<object> saveConfigCommand;
        private readonly ReactiveCommand<object> toggleVisibilityCommand;
        private readonly SerialDisposable characterSelectionDisposable = new SerialDisposable();

        private ICharacter[] charactersList;

        private bool isBusy;

        private Exception loginException;

        private ICharacter selectedCharacter;

        private string sessionId;

        private string username;

        private WindowState windowState;

        public MainWindowViewModel(
            [NotNull] IPoeBudConfigProvider<IPoeBudConfig> poeBudConfigProvider,
            [NotNull] IFactory<IPoeClient, NetworkCredential, bool> poeClientFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => poeBudConfigProvider);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => poeClientFactory);

            this.poeBudConfigProvider = poeBudConfigProvider;
            this.poeClientFactory = poeClientFactory;

            closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(CloseCommandExecuted);

            toggleVisibilityCommand = ReactiveCommand.Create();
            toggleVisibilityCommand.Subscribe(ToggleVisibilityCommandExecuted);

            saveConfigCommand = ReactiveCommand.Create();
            saveConfigCommand.Subscribe(SaveConfigCommandExecuted);

            loginCommand = ReactiveCommand.Create();
            loginCommand
                .Do(x => IsBusy = true)
                .ObserveOn(bgScheduler)
                .Do(LoginCommandExecuted)
                .ObserveOn(uiScheduler)
                .Do(x => IsBusy = false)
                .Subscribe();

            poeBudConfigProvider
                .ConfigUpdated
                .Subscribe(Refresh)
                .AddTo(Anchors);

            Observable
                .Merge(
                    this.WhenAnyValue(x => x.CharactersList).ToUnit(),
                    this.WhenAnyValue(x => x.Username).ToUnit(),
                    this.WhenAnyValue(x => x.SelectedCharacter).ToUnit())
                .Subscribe(() => this.RaisePropertyChanged(nameof(CanSave)))
                .AddTo(Anchors);

            var executingAssembly = Assembly.GetExecutingAssembly();
            WindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            var config = poeBudConfigProvider.Load();
            if (string.IsNullOrEmpty(config.CharacterName) || string.IsNullOrEmpty(config.SessionId))
            {
                WindowState = WindowState.Normal;
            }
        }

        public ICommand CloseCommand => closeCommand;

        public ICommand ToggleVisibityCommand => toggleVisibilityCommand;

        public ICommand SaveConfigCommand => saveConfigCommand;

        public ICommand LoginCommand => loginCommand;

        public string WindowTitle { get; }

        public ICharacter[] CharactersList
        {
            get { return charactersList; }
            set { this.RaiseAndSetIfChanged(ref charactersList, value); }
        }

        public ICharacter SelectedCharacter
        {
            get { return selectedCharacter; }
            set { this.RaiseAndSetIfChanged(ref selectedCharacter, value); }
        }

        private IReactiveList<TabSelectionViewModel> selectedCharacterStash;

        public IReactiveList<TabSelectionViewModel> SelectedCharacterStash
        {
            get { return selectedCharacterStash; }
            set { this.RaiseAndSetIfChanged(ref selectedCharacterStash, value); }
        }

        public string Username
        {
            get { return username; }
            set { this.RaiseAndSetIfChanged(ref username, value); }
        }

        public WindowState WindowState
        {
            get { return windowState; }
            set { this.RaiseAndSetIfChanged(ref windowState, value); }
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

        private void CloseCommandExecuted()
        {
            Environment.Exit(0);
        }

        private void ToggleVisibilityCommandExecuted()
        {
            WindowState = windowState == WindowState.Normal || windowState == WindowState.Maximized
                ? WindowState.Minimized
                : WindowState.Normal;
        }

        private void Refresh()
        {
            var config = poeBudConfigProvider.Load();

            Username = config.LoginEmail;
            SelectedCharacter = null;
            CharactersList = null;
            SelectedCharacterStash = null;
        }

        private void SaveConfigCommandExecuted()
        {
            var existingConfig = poeBudConfigProvider.Load();

            var newConfig = new PoeBudConfig();
            existingConfig.TransferPropertiesTo(newConfig);

            if (!string.IsNullOrEmpty(Username))
            {
                newConfig.LoginEmail = Username;
            }
            if (!string.IsNullOrEmpty(SessionId))
            {
                newConfig.SessionId = SessionId;
            }
            if (SelectedCharacter != null)
            {
                newConfig.CharacterName = SelectedCharacter.Name;
            }

            if (selectedCharacterStash != null)
            {
                var selectedTabs = selectedCharacterStash
                    .Select((x, idx) => new {Idx = idx, x.IsSelected})
                    .Where(x => x.IsSelected)
                    .Select(x => x.Idx)
                    .ToArray();

                newConfig.StashesToProcess.Clear();
                newConfig.StashesToProcess.AddRange(selectedTabs);
            }

            poeBudConfigProvider.Save(newConfig);
            WindowState = WindowState.Minimized;
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
                    .Select(x => new { Character = x, Stash = TryGetStash(poeClient, x.League) })
                    .Where(x => x.Stash != null && x.Character != null)
                    .ToDictionary(x => x.Character, x => new ReactiveList<TabSelectionViewModel>(x.Stash.Tabs.Select(tab => new TabSelectionViewModel(tab))));

                foreach (var kvp in stashes)
                {
                    kvp.Value.ChangeTrackingEnabled = true;
                    characterDisposable.Add(kvp.Value.ItemChanged.Subscribe(() => this.RaisePropertyChanged(nameof(CanSave))).AddTo(characterDisposable));
                }

                CharactersList = stashes.Keys.ToArray();

                this
                    .WhenAnyValue(x => x.SelectedCharacter)
                    .Subscribe(
                        character => SelectedCharacterStash = SelectedCharacter == null
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

        private IStash TryGetStash(IPoeClient client, string league)
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