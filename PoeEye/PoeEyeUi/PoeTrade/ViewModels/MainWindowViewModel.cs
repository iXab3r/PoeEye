namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(30);

        private readonly ReactiveCommand<object> closeTabCommand;

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();
        private readonly IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider;

        private readonly IFactory<MainWindowTabViewModel> tabFactory;
        private readonly ReactiveCommand<object> saveConfigCommand;
        private readonly ReactiveCommand<object> createNewTabCommand;

        private bool audioNotificationsEnabled = true;

        private EditableTuple<string, float>[] currenciesPriceInChaosOrbs;

        private bool isMainWindowActive;

        private MainWindowTabViewModel selectedItem;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeConfigProvider<IPoeEyeConfig> poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeClipboardParserViewModel clipboardParserViewModel,
            [NotNull] ProxyProviderViewModel proxyProviderViewModel,
            [NotNull] IPoeTradeCaptchaViewModel captchaViewModel,
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => proxyProviderViewModel);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => clipboardParserViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
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

            audioNotificationsManager.AddTo(Anchors);

            ApplicationUpdater = applicationUpdaterViewModel;
            applicationUpdaterViewModel.AddTo(Anchors);

            ClipboardParserViewModel = clipboardParserViewModel;
            clipboardParserViewModel.AddTo(Anchors);

            ProxyProviderViewModel = proxyProviderViewModel;
            proxyProviderViewModel.AddTo(Anchors);

            createNewTabCommand = ReactiveCommand.Create();
            createNewTabCommand
                .Subscribe(arg => CreateNewTabCommandExecuted(arg as IPoeQueryInfo))
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
                .Merge(TabsList.ItemChanged.Where(x => x.PropertyName == nameof(MainWindowTabViewModel.AudioNotificationEnabled)).ToUnit())
                .Merge(this.WhenAnyValue(x => x.AudioNotificationsEnabled).ToUnit())
                .Merge(clipboardParserViewModel.WhenAnyValue(x => x.MonitoringEnabled).ToUnit())
                .Subscribe(configUpdateSubject)
                .AddTo(Anchors);

            saveConfigCommand = ReactiveCommand.Create();
            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Merge(saveConfigCommand.ToUnit())
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, CheckForUpdatesTimeout, TaskPoolScheduler.Default)
                .ObserveOn(uiScheduler)
                .Subscribe(() => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this), Log.HandleException);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ICommand SaveConfigCommand => saveConfigCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public PoeClipboardParserViewModel ClipboardParserViewModel { get; }

        public ProxyProviderViewModel ProxyProviderViewModel { get; }

        public IPoeTradeCaptchaViewModel PoeTradeCaptchaViewModel { get; }

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

        public EditableTuple<string,float>[] CurrenciesPriceInChaosOrbs
        {
            get { return currenciesPriceInChaosOrbs; }
            set { this.RaiseAndSetIfChanged(ref currenciesPriceInChaosOrbs, value); }
        }

        public MainWindowTabViewModel SelectedItem
        {
            get { return selectedItem; }
            set { this.RaiseAndSetIfChanged(ref selectedItem, value); }
        }

        private void CreateNewTabCommandExecuted([CanBeNull] IPoeQueryInfo query)
        {
            var tab = CreateAndAddTab();

            if (query != null)
            {
                tab.QueryViewModel.SetQueryInfo(query);
            }
        }

        private MainWindowTabViewModel CreateAndAddTab()
        {
            var newTab = tabFactory.Create();

            newTab
                .QueryViewModel
                .Changed
                .Select(x => Unit.Default)
                .Subscribe(configUpdateSubject);

            TabsList.Add(newTab);
            return newTab;
        }

        private void RemoveTabCommandExecuted(MainWindowTabViewModel tab)
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
                    RecheckTimeout = tab.RecheckPeriodViewModel.RecheckValue,
                    IsAutoRecheckEnabled = tab.RecheckPeriodViewModel.IsAutoRecheckEnabled,
                    QueryInfo = tab.QueryViewModel.PoeQueryBuilder(),
                    AudioNotificationEnabled = tab.AudioNotificationEnabled,
                    SoldOrRemovedItems = tab.TradesListViewModel.HistoricalTradesViewModel.ItemsViewModels.Select(x => x.Trade).ToArray(),
                }).ToArray();

            config.AudioNotificationsEnabled = AudioNotificationsEnabled;
            config.ClipboardMonitoringEnabled = ClipboardParserViewModel.MonitoringEnabled;

            config.CurrenciesPriceInChaos = CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1, x => x.Item2);

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
                    tab.RecheckPeriodViewModel.RecheckValue = tabConfig.RecheckTimeout;
                }

                if (tabConfig.QueryInfo != null)
                {
                    tab.QueryViewModel.SetQueryInfo(tabConfig.QueryInfo);
                }

                if (tabConfig.SoldOrRemovedItems != null)
                {
                    tab.TradesListViewModel.HistoricalTradesViewModel.Clear();
                    tab.TradesListViewModel.HistoricalTradesViewModel.AddItems(tabConfig.SoldOrRemovedItems);
                }

                tab.AudioNotificationEnabled = tabConfig.AudioNotificationEnabled;
            }

            audioNotificationsEnabled = config.AudioNotificationsEnabled;
            ClipboardParserViewModel.MonitoringEnabled = config.ClipboardMonitoringEnabled;
            CurrenciesPriceInChaosOrbs = config
                .CurrenciesPriceInChaos
                .Select(x => new EditableTuple<string, float> { Item1 = x.Key, Item2 = x.Value })
                .ToArray();

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
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
    }
}