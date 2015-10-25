namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;

    using Config;

    using DumpToText;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    using TypeConverter;

    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan CheckForUpdatesTimeout = TimeSpan.FromSeconds(600);
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(30);

        private readonly ReactiveCommand<object> closeTabCommand;
        private readonly ReactiveCommand<object> createNewTabCommand;
        private readonly IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider;
        private readonly IConverter<IPoeItem, IPoeQueryInfo> itemToQueryConverter;

        private readonly IFactory<MainWindowTabViewModel> tabFactory;

        private bool audioNotificationsEnabled = true;

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();

        private bool isMainWindowActive;

        private MainWindowTabViewModel selectedItem;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeClipboardParserViewModel clipboardParserViewModel,
            [NotNull] IConverter<IPoeItem, IPoeQueryInfo> itemToQueryConverter,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => itemToQueryConverter);
            Guard.ArgumentNotNull(() => uiScheduler);

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;
            this.itemToQueryConverter = itemToQueryConverter;

            ApplicationUpdater = applicationUpdaterViewModel;
            ClipboardParserViewModel = clipboardParserViewModel;

            createNewTabCommand = ReactiveCommand.Create();
            createNewTabCommand
                .Subscribe(arg => CreateNewTabCommandExecuted(arg))
                .AddTo(Anchors);

            closeTabCommand = ReactiveCommand.Create();
            closeTabCommand
                .Where(x => x is MainWindowTabViewModel)
                .Select(x => x as MainWindowTabViewModel)
                .Subscribe(RemoveTabCommandExecuted)
                .AddTo(Anchors);

            TabsList
                .ItemsAdded
                .Subscribe(x => SelectedItem = x)
                .AddTo(Anchors);

            LoadConfig();

            if (!TabsList.Any())
            {
                CreateNewTabCommandExecuted(null);
            }

            this.WhenAnyValue(x => x.IsMainWindowActive)
                .DistinctUntilChanged()
                .Subscribe(active => audioNotificationsManager.IsEnabled = audioNotificationsEnabled && !active, Log.HandleException)
                .AddTo(Anchors);

            TabsList
                .Changed.ToUnit()
                .Merge(TabsList.ItemChanged.Where(x => x.PropertyName == nameof(MainWindowTabViewModel.RecheckTimeoutInSeconds)).ToUnit())
                .Merge(TabsList.ItemChanged.Where(x => x.PropertyName == nameof(MainWindowTabViewModel.AudioNotificationEnabled)).ToUnit())
                .Subscribe(configUpdateSubject)
                .AddTo(Anchors);

            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, CheckForUpdatesTimeout, TaskPoolScheduler.Default)
                .ObserveOn(uiScheduler)
                .Subscribe(() => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this), Log.HandleException); 
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public PoeClipboardParserViewModel ClipboardParserViewModel { get; }

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

        private void CreateNewTabCommandExecuted([CanBeNull] object arg)
        {
            var tab = CreateAndAddTab();

            if (arg is IPoeItem)
            {
                var query = itemToQueryConverter.Convert(arg as IPoeItem);
                tab.QueryViewModel.SetQueryInfo(query);
            }
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
            tab.Dispose();
        }

        private void SaveConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.SaveConfig] Saving config (provider: {poeEyeConfigProvider})...\r\nTabs count: {TabsList.Count}");

            var config = new PoeEyeConfig();
            config.TabConfigs = TabsList.Select(tab => new PoeEyeTabConfig
            {
                RecheckTimeout = tab.TradesListViewModel.RecheckTimeout,
                QueryInfo = tab.TradesListViewModel.QueryInfo,
                AudioNotificationEnabled = tab.AudioNotificationEnabled,
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

                tab.AudioNotificationEnabled = tabConfig.AudioNotificationEnabled;
            }

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
        }
    }
}