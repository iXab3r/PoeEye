﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.MetroModels;
using PoeEye.PoeTrade.Models;
using PoeEye.PoeTrade.Updater;
using PoeEye.PoeTrade.ViewModels;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using PoeShared.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Legacy;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace PoeEye.PoeTrade.Shell.ViewModels
{
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class MainWindowViewModel : DisposableReactiveObject, IMainWindowViewModel
    {
        private static readonly string ExplorerExecutablePath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");

        private static readonly TimeSpan CheckForUpdatesTimeout = TimeSpan.FromHours(1);
        private static readonly TimeSpan ConfigSaveSampingTimeout = TimeSpan.FromSeconds(10);

        private readonly ReactiveCommand<object> closeTabCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
        private readonly ReactiveCommand<object> createNewTabCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
        private readonly ReactiveCommand<Unit> refreshAllTabsCommand;

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();
        private readonly IPoeEyeMainConfigProvider poeEyeConfigProvider;

        private readonly IFactory<IMainWindowTabViewModel> tabFactory;

        private readonly ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList;
        private readonly ISourceList<IMainWindowTabViewModel> tabsListSource = new SourceList<IMainWindowTabViewModel>();

        private IMainWindowTabViewModel selectedTab;

        public MainWindowViewModel(
            [NotNull] IFactory<IMainWindowTabViewModel> tabFactory,
            [NotNull] IFactory<PoeSummaryTabViewModel, ReadOnlyObservableCollection<IMainWindowTabViewModel>> summaryTabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IPoeEyeMainConfigProvider poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeClipboardParserViewModel clipboardParserViewModel,
            [NotNull] ProxyProviderViewModel proxyProviderViewModel,
            [NotNull] PoeEyeSettingsViewModel settings,
            [NotNull] IPoeChatViewModel chatViewModel,
            [NotNull] IWhispersNotificationManager whispersNotificationManager,
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(tabFactory, nameof(tabFactory));
            Guard.ArgumentNotNull(summaryTabFactory, nameof(summaryTabFactory));
            Guard.ArgumentNotNull(applicationUpdaterViewModel, nameof(applicationUpdaterViewModel));
            Guard.ArgumentNotNull(proxyProviderViewModel, nameof(proxyProviderViewModel));
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(clipboardParserViewModel, nameof(clipboardParserViewModel));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(whispersNotificationManager, nameof(whispersNotificationManager));
            Guard.ArgumentNotNull(dialogCoordinator, nameof(dialogCoordinator));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            dialogCoordinator.MainWindow = this;

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;

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

            tabsListSource
                .Connect()
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out tabsList)
                .Subscribe()
                .AddTo(Anchors);

            SummaryTab = summaryTabFactory.Create(tabsList);
            SummaryTab.AddTo(Anchors);

            OpenAppDataDirectoryCommand = ReactiveUI.Legacy.ReactiveCommand
                .CreateAsyncTask(x => OpenAppDataDirectory());

            createNewTabCommand
                .Subscribe(arg => CreateNewTabCommandExecuted(arg as IPoeQueryInfo))
                .AddTo(Anchors);

            closeTabCommand
                .Where(x => x is IMainWindowTabViewModel)
                .Select(x => x as IMainWindowTabViewModel)
                .Subscribe(RemoveTabCommandExecuted)
                .AddTo(Anchors);

            refreshAllTabsCommand = ReactiveUI.Legacy.ReactiveCommand
                .CreateAsyncTask(_ => RefreshAllTabsCommandExecuted(), uiScheduler);

            refreshAllTabsCommand
                .Subscribe()
                .AddTo(Anchors);

            tabsListSource
                .Connect()
                .ObserveOn(uiScheduler)
                .OnItemAdded(x => SelectedTab = x)
                .Subscribe()
                .AddTo(Anchors);

            LoadConfig();

            if (tabsListSource.Count == 0)
            {
                CreateAndAddTab();
            }

            Observable.Merge(
                    tabsListSource.Connect().ToObservableChangeSet().ToUnit(),
                    tabsListSource.Connect().WhenPropertyChanged(x => x.SelectedAudioNotificationType).ToUnit()
                )
                .Do(_ => { })
                .Subscribe(configUpdateSubject)
                .AddTo(Anchors);

            configUpdateSubject
                .Sample(ConfigSaveSampingTimeout)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);
            Observable
                .Timer(DateTimeOffset.MinValue, CheckForUpdatesTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(() => applicationUpdaterViewModel.CheckForUpdatesCommand.Execute(this), Log.HandleException)
                .AddTo(Anchors);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ICommand RefreshAllTabsCommand => refreshAllTabsCommand;

        public ICommand OpenAppDataDirectoryCommand { get; }

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public PoeClipboardParserViewModel ClipboardParserViewModel { get; }

        public ProxyProviderViewModel ProxyProviderViewModel { get; }

        public IPoeChatViewModel Chat { get; }

        public PoeEyeSettingsViewModel Settings { get; }

        public PoeSummaryTabViewModel SummaryTab { get; }

        public ReadOnlyObservableCollection<IMainWindowTabViewModel> TabsList => tabsList;

        public string MainWindowTitle { get; }

        public IMainWindowTabViewModel SelectedTab
        {
            get { return selectedTab; }
            set { this.RaiseAndSetIfChanged(ref selectedTab, value); }
        }

        public IReactiveList<IPoeFlyoutViewModel> Flyouts { get; } = new ReactiveList<IPoeFlyoutViewModel>();

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

        private async Task OpenAppDataDirectory()
        {
            await Task.Run(() => Process.Start(ExplorerExecutablePath, AppArguments.AppDataDirectory));
        }

        private void CreateNewTabCommandExecuted([CanBeNull] IPoeQueryInfo query)
        {
            var tab = CreateAndAddTab();

            if (query != null)
            {
                tab.Query.SetQueryInfo(query);
            }
        }

        private Task RefreshAllTabsCommandExecuted()
        {
            foreach (var tab in TabsList.ToArray())
            {
                tab.RefreshCommand.Execute(tab.Query?.PoeQueryBuilder);
            }
            return Task.Delay(UiConstants.ArtificialLongDelay);
        }

        private IMainWindowTabViewModel CreateAndAddTab()
        {
            var newTab = tabFactory.Create();

            newTab
                .WhenAnyValue(x => x.SelectedApi)
                .ToUnit()
                .Subscribe(configUpdateSubject)
                .AddTo(newTab.Anchors);

            newTab
                .WhenAnyValue(x => x.Query)
                .Select(x => x.Changed.ToUnit())
                .Switch()
                .Subscribe(configUpdateSubject)
                .AddTo(newTab.Anchors);

            tabsListSource.Add(newTab);
            return newTab;
        }

        private void RemoveTabCommandExecuted(IMainWindowTabViewModel tab)
        {
            Log.Instance.Debug($"[MainWindowViewModel.RemoveTab] Removing tab {tab}...");
            tabsListSource.Remove(tab);
            tab.Dispose();
        }

        private void SaveConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.SaveConfig] Saving config (provider: {poeEyeConfigProvider})...\r\nTabs count: {TabsList.Count}");

            var config = poeEyeConfigProvider.ActualConfig;
            config.TabConfigs = TabsList.Select(tab => tab.Save()).ToArray();

            poeEyeConfigProvider.Save(config);
        }

        private void LoadConfig()
        {
            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Loading config (provider: {poeEyeConfigProvider})...");

            var config = poeEyeConfigProvider.ActualConfig;

            Log.Instance.Trace($"[MainWindowViewModel.LoadConfig] Received configuration DTO:\r\n{config.DumpToText()}");

            foreach (var tabConfig in config.TabConfigs)
            {
                var tab = CreateAndAddTab();
                tab.Load(tabConfig);
            }
            
            Log.Instance.Debug($"[MainWindowViewModel.LoadConfig] Sucessfully loaded config\r\nTabs count: {TabsList.Count}");
        }
    }
}
