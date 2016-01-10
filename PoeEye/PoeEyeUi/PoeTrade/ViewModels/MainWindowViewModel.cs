namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Windows.Input;

    using Config;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.DumpToText;
    using PoeShared.PoeTrade;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    using Utilities;

    internal sealed class MainWindowViewModel : DisposableReactiveObject, IMainWindowViewModel
    {
        private static readonly TimeSpan CheckForUpdatesTimeout = TimeSpan.FromSeconds(600);
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(10);

        private readonly ReactiveCommand<object> closeTabCommand = ReactiveCommand.Create();

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();
        private readonly ReactiveCommand<object> createNewTabCommand = ReactiveCommand.Create();
        private readonly IPoeEyeConfigProvider poeEyeConfigProvider;

        private readonly IFactory<IMainWindowTabViewModel> tabFactory;

        private bool audioNotificationsEnabled = true;

        private EditableTuple<string, float>[] currenciesPriceInChaosOrbs;

        private IMainWindowTabViewModel selectedItem;

        private bool settingsFlyoutIsOpen;

        private bool whisperNotificationsEnabled;

        public MainWindowViewModel(
            [NotNull] IFactory<IMainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeConfigProvider poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeClipboardParserViewModel clipboardParserViewModel,
            [NotNull] ProxyProviderViewModel proxyProviderViewModel,
            [NotNull] IPoeTradeCaptchaViewModel captchaViewModel,
            [NotNull] IWhispersNotificationManager whispersNotificationManager,
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => proxyProviderViewModel);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => clipboardParserViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => whispersNotificationManager);
            Guard.ArgumentNotNull(() => captchaViewModel);
            Guard.ArgumentNotNull(() => dialogCoordinator);
            Guard.ArgumentNotNull(() => uiScheduler);

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            dialogCoordinator.MainWindow = this;

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;

            PoeTradeCaptchaViewModel = captchaViewModel;
            captchaViewModel.AddTo(Anchors);

            ApplicationUpdater = applicationUpdaterViewModel;
            applicationUpdaterViewModel.AddTo(Anchors);

            ClipboardParserViewModel = clipboardParserViewModel;
            clipboardParserViewModel.AddTo(Anchors);

            ProxyProviderViewModel = proxyProviderViewModel;
            proxyProviderViewModel.AddTo(Anchors);

            createNewTabCommand
                .Subscribe(arg => CreateNewTabCommandExecuted(arg as IPoeQueryInfo))
                .AddTo(Anchors);

            closeTabCommand
                .Where(x => x is IMainWindowTabViewModel)
                .Select(x => x as IMainWindowTabViewModel)
                .Subscribe(RemoveTabCommandExecuted)
                .AddTo(Anchors);

            TabsList
                .ItemsAdded
                .Subscribe(x => SelectedItem = x)
                .AddTo(Anchors);

            LoadConfig();

            if (!TabsList.Any())
            {
                CreateAndAddTab();
            }

            TabsList
                .Changed.ToUnit()
                .Merge(TabsList.ItemChanged.Where(x => x.PropertyName == nameof(IMainWindowTabViewModel.AudioNotificationEnabled)).ToUnit())
                .Subscribe(configUpdateSubject)
                .AddTo(Anchors);

            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);

            this.ObservableForProperty(x => x.SettingsFlyoutIsOpen)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .Where(x => x == false)
                .ToUnit()
                .Subscribe(configUpdateSubject);

            Observable
                .Timer(DateTimeOffset.Now, CheckForUpdatesTimeout, TaskPoolScheduler.Default)
                .ObserveOn(uiScheduler)
                .Subscribe(() => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this), Log.HandleException);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public PoeClipboardParserViewModel ClipboardParserViewModel { get; }

        public ProxyProviderViewModel ProxyProviderViewModel { get; }

        public IPoeTradeCaptchaViewModel PoeTradeCaptchaViewModel { get; }

        public bool SettingsFlyoutIsOpen
        {
            get { return settingsFlyoutIsOpen; }
            set { this.RaiseAndSetIfChanged(ref settingsFlyoutIsOpen, value); }
        }

        public bool AudioNotificationsEnabled
        {
            get { return audioNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationsEnabled, value); }
        }

        public bool WhisperNotificationsEnabled
        {
            get { return whisperNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value); }
        }

        public IReactiveList<IMainWindowTabViewModel> TabsList { get; } = new ReactiveList<IMainWindowTabViewModel>
        {
            ChangeTrackingEnabled = true
        };

        public string MainWindowTitle { get; }

        public EditableTuple<string, float>[] CurrenciesPriceInChaosOrbs
        {
            get { return currenciesPriceInChaosOrbs; }
            set { this.RaiseAndSetIfChanged(ref currenciesPriceInChaosOrbs, value); }
        }

        public IMainWindowTabViewModel SelectedItem
        {
            get { return selectedItem; }
            set { this.RaiseAndSetIfChanged(ref selectedItem, value); }
        }

        public override void Dispose()
        {
            Log.Instance.Debug($"[MainWindowViewModel.Dispose] Disposing viewmodel...");

            SaveConfig();
            foreach (var mainWindowTabViewModel in TabsList)
            {
                mainWindowTabViewModel.Dispose();
            }
            base.Dispose();

            Log.Instance.Debug($"[MainWindowViewModel.Dispose] Viewmodel disposed");
        }

        private void CreateNewTabCommandExecuted([CanBeNull] IPoeQueryInfo query)
        {
            var tab = CreateAndAddTab();

            if (query != null)
            {
                tab.Query.SetQueryInfo(query);
            }
        }

        private IMainWindowTabViewModel CreateAndAddTab()
        {
            var newTab = tabFactory.Create();

            newTab
                .Query
                .Changed
                .Select(x => Unit.Default)
                .Subscribe(configUpdateSubject);

            TabsList.Add(newTab);
            return newTab;
        }

        private void RemoveTabCommandExecuted(IMainWindowTabViewModel tab)
        {
            Log.Instance.Debug($"[MainWindowViewModel.RemoveTab] Removing tab {tab}...");
            TabsList.Remove(tab);
            tab.Dispose();
        }

        private void SaveConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.SaveConfig] Saving config (provider: {poeEyeConfigProvider})...\r\nTabs count: {TabsList.Count}");

            var config = new PoeEyeConfig();
            config.TabConfigs = TabsList.Select(
                tab => new PoeEyeTabConfig
                {
                    RecheckTimeout = tab.RecheckPeriod.RecheckValue,
                    IsAutoRecheckEnabled = tab.RecheckPeriod.IsAutoRecheckEnabled,
                    QueryInfo = tab.Query.PoeQueryBuilder(),
                    AudioNotificationEnabled = tab.AudioNotificationEnabled,
                    SoldOrRemovedItems = tab.TradesList.HistoricalTradesViewModel.ItemsViewModels.Select(x => x.Trade).ToArray()
                }).ToArray();

            config.AudioNotificationsEnabled = AudioNotificationsEnabled;
            config.ClipboardMonitoringEnabled = ClipboardParserViewModel.MonitoringEnabled;
            config.WhisperNotificationsEnabled = WhisperNotificationsEnabled;

            config.CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1, x => x.Item2);

            poeEyeConfigProvider.Save(config);
        }

        private void LoadConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Loading config (provider: {poeEyeConfigProvider})...");
            var config = poeEyeConfigProvider.ActualConfig;
            poeEyeConfigProvider.Save(config);

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Received configuration DTO:\r\n{config.DumpToTextValue()}");

            foreach (var tabConfig in config.TabConfigs)
            {
                var tab = CreateAndAddTab();

                if (tabConfig.RecheckTimeout != default(TimeSpan))
                {
                    tab.RecheckPeriod.RecheckValue = tabConfig.RecheckTimeout;
                    tab.RecheckPeriod.IsAutoRecheckEnabled = tabConfig.IsAutoRecheckEnabled;
                }

                if (tabConfig.QueryInfo != null)
                {
                    tab.Query.SetQueryInfo(tabConfig.QueryInfo);
                }

                if (tabConfig.SoldOrRemovedItems != null)
                {
                    tab.TradesList.HistoricalTradesViewModel.Clear();
                    tab.TradesList.HistoricalTradesViewModel.AddItems(tabConfig.SoldOrRemovedItems);
                }

                tab.AudioNotificationEnabled = tabConfig.AudioNotificationEnabled;
            }

            AudioNotificationsEnabled = config.AudioNotificationsEnabled;
            WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;
            ClipboardParserViewModel.MonitoringEnabled = config.ClipboardMonitoringEnabled;
            CurrenciesPriceInChaosOrbs = config
                .CurrenciesPriceInChaos
                .Select(x => new EditableTuple<string, float> {Item1 = x.Key, Item2 = x.Value})
                .ToArray();

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
        }
    }
}