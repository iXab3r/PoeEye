using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Common.Logging;
using Dragablz;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeEye.PoeTrade.Models;
using PoeEye.PoeTrade.Updater;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.Utilities;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.Shell.ViewModels
{
    internal sealed class MainWindowViewModel : DisposableReactiveObject, IMainWindowViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger<MainWindowViewModel>();
        
        private static readonly int UndoStackDepth = 10;

        private static readonly string ExplorerExecutablePath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");

        private static readonly TimeSpan ConfigSaveSamplingTimeout = TimeSpan.FromSeconds(10);
        private readonly IClipboardManager clipboardManager;
        private readonly IConfigSerializer configSerializer;

        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();
        private readonly IConfigProvider<PoeEyeTabListConfig> poeEyeConfigProvider;
        private readonly TabablzPositionMonitor<IMainWindowTabViewModel> positionMonitor = new TabablzPositionMonitor<IMainWindowTabViewModel>();

        private readonly CircularBuffer<PoeEyeTabConfig> recentlyClosedQueries = new CircularBuffer<PoeEyeTabConfig>(UndoStackDepth);

        private readonly IFactory<IMainWindowTabViewModel> tabFactory;

        private readonly ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList;
        private readonly ISourceList<IMainWindowTabViewModel> tabsListSource = new SourceList<IMainWindowTabViewModel>();

        private IMainWindowTabViewModel selectedTab;

        public MainWindowViewModel(
            [NotNull] IFactory<IMainWindowTabViewModel> tabFactory,
            [NotNull] IFactory<IPoeSummaryTabViewModel, ReadOnlyObservableCollection<IMainWindowTabViewModel>> summaryTabFactory,
            [NotNull] ApplicationUpdaterViewModel applicationUpdaterViewModel,
            [NotNull] IConfigProvider<PoeEyeTabListConfig> poeEyeConfigProvider,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] ProxyProviderViewModel proxyProviderViewModel,
            [NotNull] PoeEyeSettingsViewModel settings,
            [NotNull] IPoeChatViewModel chatViewModel,
            [NotNull] IWhispersNotificationManager whispersNotificationManager,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] IConfigSerializer configSerializer,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(tabFactory, nameof(tabFactory));
            Guard.ArgumentNotNull(summaryTabFactory, nameof(summaryTabFactory));
            Guard.ArgumentNotNull(applicationUpdaterViewModel, nameof(applicationUpdaterViewModel));
            Guard.ArgumentNotNull(proxyProviderViewModel, nameof(proxyProviderViewModel));
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(clipboardManager, nameof(clipboardManager));
            Guard.ArgumentNotNull(configSerializer, nameof(configSerializer));
            Guard.ArgumentNotNull(whispersNotificationManager, nameof(whispersNotificationManager));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            var executingAssembly = Assembly.GetExecutingAssembly();
            MainWindowTitle = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            this.tabFactory = tabFactory;
            this.poeEyeConfigProvider = poeEyeConfigProvider;
            this.clipboardManager = clipboardManager;
            this.configSerializer = configSerializer;

            Chat = chatViewModel;
            chatViewModel.AddTo(Anchors);

            Settings = settings;
            settings.AddTo(Anchors);

            ApplicationUpdater = applicationUpdaterViewModel;
            applicationUpdaterViewModel.AddTo(Anchors);

            ProxyProviderViewModel = proxyProviderViewModel;
            proxyProviderViewModel.AddTo(Anchors);

            Observable
                .FromEventPattern<OrderChangedEventArgs>(
                    h => positionMonitor.OrderChanged += h,
                    h => positionMonitor.OrderChanged -= h)
                .Select(x => x.EventArgs)
                .Subscribe(OnTabOrderChanged)
                .AddTo(Anchors);

            tabsListSource
                .Connect()
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out tabsList)
                .Subscribe()
                .AddTo(Anchors);

            SummaryTab = summaryTabFactory.Create(tabsList);
            SummaryTab.AddTo(Anchors);

            OpenAppDataDirectoryCommand = CommandWrapper.Create(OpenAppDataDirectory);

            DuplicateTabCommand = CommandWrapper
                                  .Create(new DelegateCommand<IMainWindowTabViewModel>(DuplicateTabCommandExecuted, DuplicateTabCommandCanExecute))
                                  .RaiseCanExecuteChangedWhen(this.WhenAnyValue(x => x.SelectedTab).ToUnit());
            CopyTabToClipboardCommand = CommandWrapper
                                        .Create(new DelegateCommand<IMainWindowTabViewModel>(CopyTabToClipboardExecuted, CopyTabToClipboardCommandCanExecute))
                                        .RaiseCanExecuteChangedWhen(this.WhenAnyValue(x => x.SelectedTab).ToUnit());
            PasteTabCommand = CommandWrapper.Create(new DelegateCommand(PasteTabCommandExecuted));

            CreateNewTabCommand = CommandWrapper.Create(new DelegateCommand(() => CreateNewTabCommandExecuted(default(PoeEyeTabConfig))));
            CloseTabCommand = CommandWrapper.Create(new DelegateCommand<IMainWindowTabViewModel>(RemoveTabCommandExecuted, RemoveTabCommandCanExecute));
            UndoCloseTabCommand = CommandWrapper.Create(new DelegateCommand(UndoCloseTabCommandExecuted, UndoCloseTabCommandCanExecute));

            RefreshAllTabsCommand = CommandWrapper.Create(RefreshAllTabsCommandExecuted);

            tabsListSource
                .Connect()
                .ObserveOn(uiScheduler)
                .OnItemAdded(x => SelectedTab = x)
                .Subscribe()
                .AddTo(Anchors);

            LoadConfig();

            if (tabsListSource.Count == 0)
            {
                CreateNewTabCommand.Execute(null);
            }

            Observable.Merge(
                          tabsListSource.Connect().ToUnit(),
                          tabsListSource.Connect().WhenPropertyChanged(x => x.SelectedAudioNotificationType).ToUnit(),
                          tabsListSource.Connect().WhenPropertyChanged(x => x.TabName).ToUnit()
                      )
                      .Subscribe(configUpdateSubject)
                      .AddTo(Anchors);

            configUpdateSubject
                .Sample(ConfigSaveSamplingTimeout)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);
        }

        public CommandWrapper CreateNewTabCommand { get; }

        public CommandWrapper CopyTabToClipboardCommand { get; }

        public CommandWrapper DuplicateTabCommand { get; }

        public CommandWrapper UndoCloseTabCommand { get; }

        public CommandWrapper PasteTabCommand { get; }

        public CommandWrapper CloseTabCommand { get; }

        public CommandWrapper RefreshAllTabsCommand { get; }

        public CommandWrapper OpenAppDataDirectoryCommand { get; }

        public ApplicationUpdaterViewModel ApplicationUpdater { get; }

        public ProxyProviderViewModel ProxyProviderViewModel { get; }

        public IPoeChatViewModel Chat { get; }

        public PoeEyeSettingsViewModel Settings { get; }

        public IPoeSummaryTabViewModel SummaryTab { get; }

        public PositionMonitor PositionMonitor => positionMonitor;

        public string MainWindowTitle { get; }

        public IMainWindowTabViewModel SelectedTab
        {
            get => selectedTab;
            set => this.RaiseAndSetIfChanged(ref selectedTab, value);
        }

        public ReadOnlyObservableCollection<IMainWindowTabViewModel> TabsList => tabsList;

        public override void Dispose()
        {
            Log.Debug("[MainWindowViewModel.Dispose] Disposing viewmodel...");
            SaveConfig();
            foreach (var mainWindowTabViewModel in TabsList)
            {
                mainWindowTabViewModel.Dispose();
            }

            base.Dispose();

            Log.Debug("[MainWindowViewModel.Dispose] Viewmodel disposed");
        }


        private bool UndoCloseTabCommandCanExecute()
        {
            return !recentlyClosedQueries.IsEmpty;
        }

        private void UndoCloseTabCommandExecuted()
        {
            if (!UndoCloseTabCommandCanExecute())
            {
                return;
            }

            var query = recentlyClosedQueries.PopBack();
            CreateNewTabCommandExecuted(query);
            UndoCloseTabCommand.RaiseCanExecuteChanged();
        }

        private void PasteTabCommandExecuted()
        {
            var content = clipboardManager.GetText();
            var cfg = configSerializer.Decompress<PoeEyeTabConfig>(content);
            CreateNewTabCommandExecuted(cfg);
        }

        private bool DuplicateTabCommandCanExecute(IMainWindowTabViewModel tab)
        {
            return tab != null;
        }

        private void DuplicateTabCommandExecuted(IMainWindowTabViewModel tab)
        {
            Guard.ArgumentIsTrue(() => DuplicateTabCommandCanExecute(tab));

            var cfg = tab.Save();
            CreateNewTabCommandExecuted(cfg);
        }

        private void OnTabOrderChanged(OrderChangedEventArgs args)
        {
            var existingItems = tabsListSource.Items.ToList();
            var newItems = args.NewOrder.OfType<IMainWindowTabViewModel>().ToList();

            Log.Debug(
                $"[PositionMonitor] Source ordering:\n\tSource: {string.Join(" => ", existingItems.Select(x => x.Id))}\n\tView: {string.Join(" => ", newItems.Select(x => x.Id))}");
            configUpdateSubject.OnNext(Unit.Default);
        }

        private async Task OpenAppDataDirectory()
        {
            await Task.Run(() => Process.Start(ExplorerExecutablePath, AppArguments.AppDataDirectory));
        }

        private void CreateNewTabCommandExecuted(PoeEyeTabConfig cfg)
        {
            var tab = CreateAndAddTab();
            if (default(PoeEyeTabConfig).Equals(cfg))
            {
                cfg = poeEyeConfigProvider.ActualConfig.DefaultConfig;
            }
            tab.Load(cfg);
        }

        private async Task RefreshAllTabsCommandExecuted()
        {
            foreach (var tab in TabsList.Where(x => x.RecheckPeriod?.Period >= TimeSpan.Zero).ToArray())
            {
                tab.RefreshCommand.Execute(tab.Query?.PoeQueryBuilder);
            }

            await Task.Delay(UiConstants.ArtificialLongDelay);
        }

        private IMainWindowTabViewModel CreateAndAddTab()
        {
            var newTab = tabFactory.Create();

            newTab.RecheckPeriod.Period = TimeSpan.MinValue; // by default, recheck is disabled

            newTab
                .WhenAnyValue(x => x.SelectedApi)
                .ToUnit()
                .Subscribe(configUpdateSubject)
                .AddTo(newTab.Anchors);

            newTab
                .WhenAnyValue(x => x.Query)
                .Select(x => x != null ? x.Changed.ToUnit() : Observable.Return(Unit.Default).Concat(Observable.Never<Unit>()))
                .Switch()
                .Subscribe(configUpdateSubject)
                .AddTo(newTab.Anchors);

            tabsListSource.Add(newTab);
            return newTab;
        }

        private bool RemoveTabCommandCanExecute(IMainWindowTabViewModel tab)
        {
            return tab != null;
        }

        private void RemoveTabCommandExecuted(IMainWindowTabViewModel tab)
        {
            Guard.ArgumentIsTrue(() => RemoveTabCommandCanExecute(tab));

            Log.Debug($"[MainWindowViewModel.RemoveTab] Removing tab {tab}...");

            var items = positionMonitor.Items.ToArray();
            var tabIdx = items.IndexOf(tab);
            if (tabIdx > 0)
            {
                var tabToSelect = items[tabIdx - 1];
                Log.Debug($"[MainWindowViewModel.RemoveTab] Selecting neighbour tab {tabToSelect}...");
                SelectedTab = tabToSelect;
            }

            tabsListSource.Remove(tab);

            var cfg = tab.Save();
            recentlyClosedQueries.PushBack(cfg);
            UndoCloseTabCommand.RaiseCanExecuteChanged();

            tab.Dispose();
        }

        private bool CopyTabToClipboardCommandCanExecute(IMainWindowTabViewModel tab)
        {
            return tab != null;
        }

        private void CopyTabToClipboardExecuted(IMainWindowTabViewModel tab)
        {
            Guard.ArgumentIsTrue(() => CopyTabToClipboardCommandCanExecute(tab));

            Log.Debug($"[MainWindowViewModel.CopyTabToClipboard] Copying tab {tab}...");

            var cfg = tab.Save();
            var data = configSerializer.Compress(cfg);
            clipboardManager.SetText(data);
        }

        private void SaveConfig()
        {
            Log.Debug($"[MainWindowViewModel.SaveConfig] Saving config (provider: {poeEyeConfigProvider})...\r\nTabs count: {TabsList.Count}");

            var config = poeEyeConfigProvider.ActualConfig;

            var positionedItems = positionMonitor.Items.ToArray();
            config.TabConfigs = tabsListSource.Items
                                              .Select(x => new {Idx = positionedItems.IndexOf(x), Tab = x})
                                              .OrderBy(x => x.Idx)
                                              .Select(x => x.Tab)
                                              .Select(tab => tab.Save())
                                              .ToArray();

            poeEyeConfigProvider.Save(config);
        }

        private void LoadConfig()
        {
            Log.Debug($"[MainWindowViewModel.LoadConfig] Loading config (provider: {poeEyeConfigProvider})...");

            var config = poeEyeConfigProvider.ActualConfig;

            Log.Trace($"[MainWindowViewModel.LoadConfig] Received configuration DTO:\r\n{config.DumpToText()}");

            foreach (var tabConfig in config.TabConfigs)
            {
                var tab = CreateAndAddTab();
                tab.Load(tabConfig);
            }

            Log.Debug($"[MainWindowViewModel.LoadConfig] Successfully loaded config\r\nTabs count: {TabsList.Count}");
        }
    }
}