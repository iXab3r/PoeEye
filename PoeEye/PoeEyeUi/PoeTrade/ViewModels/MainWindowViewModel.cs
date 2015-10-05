namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Windows.Input;

    using Config;

    using DumpToText;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;

    using ReactiveUI;

    internal sealed class MainWindowViewModel : ReactiveObject
    {
        private static readonly TimeSpan CheckForUpdatesTimeout = TimeSpan.FromSeconds(600);
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(30);

        private readonly ReactiveCommand<object> closeTabCommand;
        private readonly ReactiveCommand<object> createNewTabCommand;
        private readonly IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider;

        private readonly IFactory<MainWindowTabViewModel> tabFactory;

        private bool audioNotificationsEnabled = true;

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();

        private bool isMainWindowActive;

        private MainWindowTabViewModel selectedItem;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => audioNotificationsManager);

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;
            ApplicationUpdater = applicationUpdaterViewModel;
            createNewTabCommand = ReactiveCommand.Create();
            createNewTabCommand.Subscribe(_ => CreateNewTabCommandExecuted());

            closeTabCommand = ReactiveCommand.Create();
            closeTabCommand
                .Where(x => x is MainWindowTabViewModel)
                .Select(x => x as MainWindowTabViewModel)
                .Subscribe(RemoveTabCommandExecuted);

            TabsList
                .ItemsAdded
                .Subscribe(x => SelectedItem = x);

            LoadConfig();

            if (!TabsList.Any())
            {
                CreateNewTabCommandExecuted();
            }

            this.WhenAnyValue(x => x.IsMainWindowActive)
                .DistinctUntilChanged()
                .Subscribe(active => audioNotificationsManager.IsEnabled = audioNotificationsEnabled && !active);

            TabsList
                .Changed
                .Select(x => Unit.Default).Merge(TabsList
                    .ItemChanged
                    .Where(x => x.PropertyName == nameof(MainWindowTabViewModel.RecheckTimeoutInSeconds))
                    .Select(x => Unit.Default)
                ).Subscribe(configUpdateSubject);

            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Subscribe(_ => SaveConfig());

            /*Observable
                .Timer(DateTimeOffset.Now, CheckForUpdatesTimeout)
                .Subscribe(_ => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this));*/    
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public bool AudioNotificationsEnabled
        {
            get { return audioNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationsEnabled, value); }
        }

        public ReactiveList<MainWindowTabViewModel> TabsList { get; } = new ReactiveList<MainWindowTabViewModel>
        {
            ChangeTrackingEnabled = true
        };

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

        private void CreateNewTabCommandExecuted()
        {
            CreateAndAddTab();
        }

        private MainWindowTabViewModel CreateAndAddTab()
        {
            var newTab = tabFactory.Create();

            newTab
                .TradesListViewModel
                .WhenAnyValue(x => x.QueryInfo)
                .Select(x => Unit.Default)
                .Subscribe(configUpdateSubject);

            TabsList.Add(newTab);
            return newTab;
        }

        private void RemoveTabCommandExecuted(MainWindowTabViewModel tab)
        {
            TabsList.Remove(tab);
        }

        private void SaveConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.SaveConfig] Saving config (provider: {poeEyeConfigProvider})...\r\nTabs count: {TabsList.Count}");

            var config = new PoeEyeConfig();
            config.TabConfigs = TabsList.Select(tab => new PoeEyeTabConfig
            {
                RecheckTimeout = tab.TradesListViewModel.RecheckTimeout,
                QueryInfo = tab.TradesListViewModel.QueryInfo
            }).ToArray();

            poeEyeConfigProvider.Save(config);
        }

        private void LoadConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Loading config (provider: {poeEyeConfigProvider})...");
            var config = poeEyeConfigProvider.Load();

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Received configuration DTO:\r\n{config.DumpToTextValue()}");

            foreach (var tabConfig in config.TabConfigs)
            {
                var tab = CreateAndAddTab();

                if (tabConfig.RecheckTimeout != default(TimeSpan))
                {
                    tab.TradesListViewModel.RecheckTimeout = tabConfig.RecheckTimeout;
                }

                if (tabConfig.QueryInfo != default(IPoeQueryInfo))
                {
                    tab.QueryViewModel.SetQueryInfo(tabConfig.QueryInfo);
                }
            }

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
        }

    }
}