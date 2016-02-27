﻿namespace PoeEyeUi.PoeTrade.ViewModels
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

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.PoeTrade;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    using Utilities;

    internal sealed class MainWindowViewModel : DisposableReactiveObject, IMainWindowViewModel
    {
        private static readonly TimeSpan CheckForUpdatesTimeout = TimeSpan.FromSeconds(600);
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(10);

        private readonly ReactiveCommand<object> closeTabCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> createNewTabCommand = ReactiveCommand.Create();

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();
        private readonly IPoeEyeConfigProvider poeEyeConfigProvider;

        private readonly IFactory<IMainWindowTabViewModel> tabFactory;

        private IMainWindowTabViewModel selectedItem;

        public MainWindowViewModel(
            [NotNull] IFactory<IMainWindowTabViewModel> tabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeConfigProvider poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeClipboardParserViewModel clipboardParserViewModel,
            [NotNull] ProxyProviderViewModel proxyProviderViewModel,
            [NotNull] PoeEyeSettingsViewModel settings,
            [NotNull] IPoeTradeCaptchaViewModel captchaViewModel,
            [NotNull] IPoeChatViewModel chatViewModel,
            [NotNull] IWhispersNotificationManager whispersNotificationManager,
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            Guard.ArgumentNotNull(() => applicationUpdaterViewModel);
            Guard.ArgumentNotNull(() => proxyProviderViewModel);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => clipboardParserViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => settings);
            Guard.ArgumentNotNull(() => whispersNotificationManager);
            Guard.ArgumentNotNull(() => captchaViewModel);
            Guard.ArgumentNotNull(() => dialogCoordinator);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            dialogCoordinator.MainWindow = this;

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;

            PoeTradeCaptchaViewModel = captchaViewModel;
            captchaViewModel.AddTo(Anchors);

            Chat = chatViewModel;
            chatViewModel.AddTo(Anchors);

            Settings = settings;
            settings.AddTo(Anchors);

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

            settings.ObservableForProperty(x => x.IsOpen)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .Where(x => x == false)
                .ToUnit()
                .Subscribe(configUpdateSubject)
                .AddTo(Anchors);

            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.MinValue, CheckForUpdatesTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(() => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this), Log.HandleException);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public PoeClipboardParserViewModel ClipboardParserViewModel { get; }

        public ProxyProviderViewModel ProxyProviderViewModel { get; }

        public IPoeTradeCaptchaViewModel PoeTradeCaptchaViewModel { get; }

        public IPoeChatViewModel Chat { get; }

        public PoeEyeSettingsViewModel Settings { get; }

        public IReactiveList<IMainWindowTabViewModel> TabsList { get; } = new ReactiveList<IMainWindowTabViewModel>
        {
            ChangeTrackingEnabled = true
        };

        public string MainWindowTitle { get; }

        

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

            var config = new PoeEyeConfig
            {
                TabConfigs = TabsList.Select(tab => tab.Save()).ToArray(),
                AudioNotificationsEnabled = Settings.AudioNotificationsEnabled,
                ClipboardMonitoringEnabled = Settings.ClipboardMonitoringEnabled,
                WhisperNotificationsEnabled = Settings.WhisperNotificationsEnabled,
                CurrenciesPriceInChaos = Settings.CurrenciesPriceInChaosOrbs.ToDictionary(x => x.Item1, x => x.Item2)
            };

            poeEyeConfigProvider.Save(config);
        }

        private void LoadConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Loading config (provider: {poeEyeConfigProvider})...");

            var config = poeEyeConfigProvider.ActualConfig;
            poeEyeConfigProvider.Save(config);

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Received configuration DTO:\r\n{config.DumpToText()}");

            foreach (var tabConfig in config.TabConfigs)
            {
                var tab = CreateAndAddTab();
                tab.Load(tabConfig);
            }

            Settings.ClipboardMonitoringEnabled = config.ClipboardMonitoringEnabled;
            Settings.AudioNotificationsEnabled = config.AudioNotificationsEnabled;
            Settings.WhisperNotificationsEnabled = config.WhisperNotificationsEnabled;
            Settings.CurrenciesPriceInChaosOrbs = config
                .CurrenciesPriceInChaos
                .Select(x => new EditableTuple<string, float> {Item1 = x.Key, Item2 = x.Value})
                .ToArray();

            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
        }
    }
}