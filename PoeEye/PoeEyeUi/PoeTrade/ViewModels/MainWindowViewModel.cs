namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using ReactiveUI;

    internal sealed class MainWindowViewModel : ReactiveObject
    {
        private readonly ReactiveCommand<object> closeTabCommand;
        private readonly ReactiveCommand<object> createNewTabCommand;

        private readonly IFactory<MainWindowTabViewModel> tabFactory;
        private readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(30);

        private bool audioNotificationsEnabled = true;

        private bool isMainWindowActive;

        private MainWindowTabViewModel selectedItem;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            this.tabFactory = tabFactory;
            ApplicationUpdater = applicationUpdaterViewModel;
            createNewTabCommand = ReactiveCommand.Create();
            createNewTabCommand.Subscribe(CreateNewTabCommandExecuted);

            closeTabCommand = ReactiveCommand.Create();
            closeTabCommand.Subscribe(RemoveTabCommandExecuted);

            Observable
                .Timer(DateTimeOffset.Now, UpdateTimeout)
                .Subscribe(_ => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this));

            TabsList
                .ItemsAdded
                .Subscribe(x => SelectedItem = x);

            createNewTabCommand.Execute(null);

            this.WhenAnyValue(x => x.IsMainWindowActive)
                .DistinctUntilChanged()
                .Subscribe(active => audioNotificationsManager.IsEnabled = audioNotificationsEnabled && !active);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public bool AudioNotificationsEnabled
        {
            get { return audioNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationsEnabled, value); }
        }

        public ReactiveList<MainWindowTabViewModel> TabsList { get; } = new ReactiveList<MainWindowTabViewModel>();

        public string MainWindowTitle { get; }

        public bool IsMainWindowActive
        {
            get { return isMainWindowActive; }
            set { this.RaiseAndSetIfChanged(ref isMainWindowActive, value); }
        }

        public MainWindowTabViewModel SelectedItem
        {
            get { return selectedItem; }
            set { this.RaiseAndSetIfChanged(ref selectedItem, value); }
        }

        private void CreateNewTabCommandExecuted(object o)
        {
            var newTab = tabFactory.Create();
            TabsList.Add(newTab);
        }

        private void RemoveTabCommandExecuted(object o)
        {
            var tab = o as MainWindowTabViewModel;
            if (tab == null)
            {
                return;
            }
            TabsList.Remove(tab);
        }
    }
}