using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Anotar.Log4Net;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Scaffolding;
using PoeBud.Services;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;
using WinFormsKeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudViewModel : OverlayViewModelBase
    {
        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(1);
        private readonly IHighlightingService highlightingService;
        private readonly IClock clock;
        private readonly ISubject<Exception> exceptionsToPropagate = new Subject<Exception>();
        private readonly IKeyboardEventsSource keyboardMouseEvents;
        private readonly IUiOverlaysProvider overlaysProvider;
        private readonly ObservableAsPropertyHelper<Exception> lastUpdateException;
        private readonly IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashAnalyzerFactory;
        private readonly IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory;
        private readonly IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory;
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IScheduler uiScheduler;

        private PoeBudConfig actualConfig;
        private bool hideXpBar;
        private KeyGesture hotkey;
        private bool isEnabled;
        private StashUpdate lastServerStashUpdate;
        private StashViewModel stash;
        private IPoeStashUpdater stashUpdater;

        private string uiOverlayPath;

        public PoeBudViewModel(
            [NotNull] IPoeWindowManager windowManager,
            [NotNull] IHighlightingService highlightingService,
            [NotNull] ISolutionExecutorViewModel solutionExecutor,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] IClock clock,
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IUserInteractionsManager userInteractionsManager,
            [NotNull] IUiOverlaysProvider overlaysProvider,
            [NotNull] IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashAnalyzerFactory,
            [NotNull] IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory,
            [NotNull] IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(windowManager, nameof(windowManager));
            Guard.ArgumentNotNull(solutionExecutor, nameof(solutionExecutor));
            Guard.ArgumentNotNull(highlightingService, nameof(highlightingService));
            Guard.ArgumentNotNull(overlaysProvider, nameof(overlaysProvider));
            Guard.ArgumentNotNull(userInteractionsManager, nameof(userInteractionsManager));
            Guard.ArgumentNotNull(stashUpdaterStrategyFactory, nameof(stashUpdaterStrategyFactory));
            Guard.ArgumentNotNull(poeBudConfigProvider, nameof(poeBudConfigProvider));
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(stashAnalyzerFactory, nameof(stashAnalyzerFactory));
            Guard.ArgumentNotNull(stashUpdateFactory, nameof(stashUpdateFactory));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            OverlayMode = OverlayMode.Layered;
            this.highlightingService = highlightingService;
            this.clock = clock;
            this.keyboardMouseEvents = keyboardMouseEvents;
            this.overlaysProvider = overlaysProvider;
            this.stashAnalyzerFactory = stashAnalyzerFactory;
            this.stashUpdaterStrategyFactory = stashUpdaterStrategyFactory;
            this.stashUpdateFactory = stashUpdateFactory;
            this.uiScheduler = uiScheduler;

            SolutionExecutor = solutionExecutor;
            WindowManager = windowManager;

            poeBudConfigProvider
                .WhenChanged
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            exceptionsToPropagate
                .ToProperty(this, x => x.LastUpdateException, out lastUpdateException, null, false, uiScheduler)
                .AddTo(Anchors);

            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Left = 0;
            Top = 0;
        }

        public IPoeWindowManager WindowManager { get; }

        public Exception LastUpdateException => lastUpdateException?.Value;

        public ISolutionExecutorViewModel SolutionExecutor { get; }

        public IPoeStashUpdater StashUpdater
        {
            get { return stashUpdater; }
            set { this.RaiseAndSetIfChanged(ref stashUpdater, value); }
        }

        public string UiOverlayPath
        {
            get { return uiOverlayPath; }
            set { this.RaiseAndSetIfChanged(ref uiOverlayPath, value); }
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

        public StashViewModel Stash
        {
            get { return stash; }
            set { this.RaiseAndSetIfChanged(ref stash, value); }
        }

        public string League => actualConfig?.LeagueId;

        public TimeSpan TimeTillNextUpdate
            =>
                stashUpdater == null || stashUpdater.LastUpdateTimestamp == DateTime.MinValue
                    ? TimeSpan.Zero
                    : stashUpdater.LastUpdateTimestamp + (actualConfig?.StashUpdatePeriod ?? TimeSpan.Zero) - clock.Now;
        
        private void ApplyConfig(PoeBudConfig config)
        {
            actualConfig = config;
            HideXpBar = config.HideXpBar;
            IsEnabled = actualConfig.IsEnabled;
            UiOverlayPath = overlaysProvider.OverlaysList.FirstOrDefault(x => x.Name == config.UiOverlayName).AbsolutePath;
            hotkey = KeyGestureExtensions.SafeCreateGesture(config.GetChaosSetHotkey);
            RefreshStashUpdater(actualConfig);
            this.RaisePropertyChanged(nameof(League));
        }

        private void RefreshStashUpdater(PoeBudConfig config)
        {
            var stashDisposable = new CompositeDisposable();

            try
            {
                Log.Instance.Info($"[MainViewModel] Reinitializing PoeBud...");
                stashUpdaterDisposable.Disposable = null;
                StashUpdater = null;
                Stash = null;
                lastServerStashUpdate = null;

                if (string.IsNullOrEmpty(config.LoginEmail) || string.IsNullOrEmpty(config.SessionId))
                {
                    Log.Instance.Warn($"[MainViewModel] Credentials are not set, userName: {config.LoginEmail}, sessionId: {config.SessionId}");
                    return;
                }

                if (!config.IsEnabled)
                {
                    Log.Instance.Debug($"[MainViewModel] PoeBud is disabled, terminating...");
                    return;
                }

                var keyPressedObservable = keyboardMouseEvents
                    .WhenKeyDown
                    .Where(x => IsEnabled)
                    .Where(x => !SolutionExecutor.IsBusy)
                    .Where(x => WindowManager.ActiveWindow != null)
                    .Publish();
                keyPressedObservable.Connect().AddTo(stashDisposable);

                keyPressedObservable
                    .Where(x => hotkey.MatchesHotkey(x))
                    .Do(x => x.Handled = true)
                    .ObserveOn(uiScheduler)
                    .Subscribe(ExecuteSolutionCommandExecuted)
                    .AddTo(stashDisposable);

                var forceRefreshHotkey = new KeyGesture(hotkey.Key, ModifierKeys.Control | ModifierKeys.Shift);
                keyPressedObservable
                   .Where(x => forceRefreshHotkey.MatchesHotkey(x))
                   .Do(x => x.Handled = true)
                   .ObserveOn(uiScheduler)
                   .Subscribe(ForceRefreshStashCommandExecuted)
                   .AddTo(stashDisposable);
                
                var highlightHotkey = new KeyGesture(hotkey.Key, ModifierKeys.Shift);
                keyPressedObservable
                    .Where(x => highlightHotkey.MatchesHotkey(x))
                    .Where(x => stash?.ChaosSetSolutions.Any() ?? false)
                    .Do(x => x.Handled = true)
                    .Select(x => stash?.ChaosSetSolutions.FirstOrDefault())
                    .ObserveOn(uiScheduler)
                    .Subscribe(HighlightSolutionCommandExecuted)
                    .AddTo(stashDisposable);

                var updater = stashAnalyzerFactory.Create(config);
                stashDisposable.Add(updater);

                var strategy = stashUpdaterStrategyFactory.Create(config);
                updater.SetStrategy(strategy);

                Observable.Timer(DateTimeOffset.Now, UpdateTimeout)
                    .ToUnit()
                    .Merge(updater.WhenAnyValue(x => x.LastUpdateTimestamp).ToUnit())
                    .ObserveOn(uiScheduler)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(TimeTillNextUpdate)))
                    .AddTo(stashDisposable);

                updater
                    .Updates
                    .ObserveOn(uiScheduler)
                    .Subscribe(update => HandleStashUpdate(update, config))
                    .AddTo(stashDisposable);

                updater.UpdateExceptions
                    .Subscribe(exceptionsToPropagate)
                    .AddTo(stashDisposable);

                updater.RecheckPeriod = config.StashUpdatePeriod;

                StashUpdater = updater;
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
                exceptionsToPropagate.OnNext(ex);
            }
            finally
            {
                stashUpdaterDisposable.Disposable = stashDisposable;
            }
        }

        private void HandleStashUpdate(StashUpdate stashUpdate, PoeBudConfig config)
        {
            if (lastServerStashUpdate != null && lastServerStashUpdate.DumpToText() == stashUpdate.DumpToText())
            {
                Log.Instance.Debug($"[MainViewModel] Duplicate update arrived, skipping update");
                return;
            }
            LogTo.Debug($"[MainViewModel] Stash update arrived, tabs: {stashUpdate.Tabs.Count()}, items: {stashUpdate.Items.Count()}");

            lastServerStashUpdate = stashUpdate;
            Stash = stashUpdateFactory.Create(stashUpdate, config);
        }

        private void ForceRefreshStashCommandExecuted()
        {
            try
            {
                Log.Instance.Debug($"[MainViewModel] Force refresh requested");
                var updater = StashUpdater;
                if (updater == null)
                {
                    return;
                }
                updater.ForceRefresh();
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }

        private void HighlightSolutionCommandExecuted(IPoeTradeSolution solution)
        {
            Guard.ArgumentNotNull(solution, nameof(solution));
            try
            {
                Log.Instance.Debug($"[MainViewModel] Highlighting solution {solution}");

                highlightingService.Highlight(solution);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }

        private async void ExecuteSolutionCommandExecuted()
        {
            try
            {
                if (SolutionExecutor.IsBusy)
                {
                    Log.Instance.Debug(
                        "[MainViewModel.ExecuteSolutionCommandExecuted] Solution executor is busy, ignoring request");
                    return;
                }

                await TryToExecuteSolution(actualConfig);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }

        private async Task<bool> TryToExecuteSolution(PoeBudConfig config)
        {
            var stashSnapshot = stash;

            var solutionToExecute = stashSnapshot?.ChaosSetSolutions.FirstOrDefault();
            if (solutionToExecute == null)
            {
                SolutionExecutor.LogOperation("Failed to find a solution, not enough items ?");
                return false;
            }

            var window = WindowManager.ActiveWindow;
            if (window == null)
            {
                SolutionExecutor.LogOperation("Path of Exile window is not active");
                return false;
            }

            await SolutionExecutor.ExecuteSolution(solutionToExecute);
            PerformDirtyUpdate(stashSnapshot, solutionToExecute, config);

            return true;
        }

        private void PerformDirtyUpdate(StashViewModel dirtyStash, IPoeTradeSolution executedSolution,
            PoeBudConfig config)
        {
            // taking ACTUAL snapshot and performing a clean-up
            if (dirtyStash != stash)
            {
                Log.Instance.Warn(
                    $"[MainViewModel.Sell] Possible race condition, trying to resolve it...");
            }
            var dirtyItems = stash
                .StashUpdate
                .Items
                .Where(item => !executedSolution.Items.Any(tradeItem => IsMatch(tradeItem, item)))
                .ToArray();

            Log.Instance.Debug($"[MainViewModel.Sell] Solution executed successfully, preparing DIRTY stash update...");

            var dirtyStashUpdate = new StashUpdate(dirtyItems, stash.StashUpdate.Tabs);
            Stash = stashUpdateFactory.Create(dirtyStashUpdate, config);
        }

        private bool IsMatch(IPoeSolutionItem solutionItem, IStashItem item)
        {
            return solutionItem.Tab.GetInventoryId() == item.InventoryId && solutionItem.Position.X == item.Position.X && solutionItem.Position.Y == item.Position.Y;
        }
    }
}
