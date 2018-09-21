using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Services;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity.Attributes;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;
using WinFormsKeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudViewModel : OverlayViewModelBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeBudViewModel));

        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(1);
        private readonly IClock clock;
        private readonly IConfigProvider<PoeBudConfig> configProvider;
        private readonly ISubject<Exception> exceptionsToPropagate = new Subject<Exception>();
        private readonly IHighlightingService highlightingService;
        private readonly IKeyboardEventsSource keyboardMouseEvents;
        private readonly ObservableAsPropertyHelper<Exception> lastUpdateException;
        private readonly IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashAnalyzerFactory;
        private readonly IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory;
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory;
        private readonly IScheduler uiScheduler;

        private PoeBudConfig actualConfig;
        private bool hideXpBar;
        private KeyGesture hotkey;
        private bool isEnabled;
        private StashUpdate lastServerStashUpdate;
        private StashViewModel stash;
        private IPoeStashUpdater stashUpdater;

        public PoeBudViewModel(
            [NotNull] IPoeWindowManager windowManager,
            [NotNull] IHighlightingService highlightingService,
            [NotNull] ISolutionExecutorViewModel solutionExecutor,
            [NotNull] IConfigProvider<PoeBudConfig> configProvider,
            [NotNull] IClock clock,
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IUserInteractionsManager userInteractionsManager,
            [NotNull] IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashAnalyzerFactory,
            [NotNull] IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory,
            [NotNull] IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(windowManager, nameof(windowManager));
            Guard.ArgumentNotNull(solutionExecutor, nameof(solutionExecutor));
            Guard.ArgumentNotNull(highlightingService, nameof(highlightingService));
            Guard.ArgumentNotNull(userInteractionsManager, nameof(userInteractionsManager));
            Guard.ArgumentNotNull(stashUpdaterStrategyFactory, nameof(stashUpdaterStrategyFactory));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(stashAnalyzerFactory, nameof(stashAnalyzerFactory));
            Guard.ArgumentNotNull(stashUpdateFactory, nameof(stashUpdateFactory));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.highlightingService = highlightingService;
            this.configProvider = configProvider;
            this.clock = clock;
            this.keyboardMouseEvents = keyboardMouseEvents;
            this.stashAnalyzerFactory = stashAnalyzerFactory;
            this.stashUpdaterStrategyFactory = stashUpdaterStrategyFactory;
            this.stashUpdateFactory = stashUpdateFactory;
            this.uiScheduler = uiScheduler;

            SolutionExecutor = solutionExecutor;
            WindowManager = windowManager;

            OverlayMode = OverlayMode.Layered;
            MinSize = new Size(100, 30);
            MaxSize = new Size(1024, 400);
            Top = 100;
            Left = 100;
            SizeToContent = SizeToContent.Manual;
            IsUnlockable = true;
            Title = "PoeBud";

            configProvider
                .WhenChanged
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            exceptionsToPropagate
                .ToProperty(this, x => x.LastUpdateException, out lastUpdateException, null, false, uiScheduler)
                .AddTo(Anchors);

            WithdrawChaosSetCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.ChaosSetSolutions.FirstOrDefault(),
                                                                           () => "Failed to withdraw ChaosSet, not enough items ?")));
            WithdrawCurrencyCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.CurrencySolutions.FirstOrDefault(),
                                                                           () => "Failed to withdraw Currency")));
            WithdrawDivinationCardsCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.DivinationCardsSolutions.FirstOrDefault(),
                                                                           () => "Failed to withdraw Divination Cards")));
            WithdrawMapsCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.MapsSolutions.FirstOrDefault(), () => "Failed to withdraw Maps")));
            WithdrawMiscCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.MiscellaneousItemsSolutions.FirstOrDefault(),
                                                                           () => "Failed to withdraw miscellaneous items")));
            WithdrawSellablesCommand = CommandWrapper.Create(
                ReactiveCommand.CreateFromTask(() => ExecuteSolutionOrFail(() => Stash.SellableSolutions.FirstOrDefault(),
                                                                           () => "Failed to withdraw sellables (six-socket, chrome, etc)")));
            ForceRefreshCommand = CommandWrapper.Create(
                ReactiveCommand.Create(ForceRefreshStashCommandExecuted));
        }

        public IPoeWindowManager WindowManager { get; }

        public Exception LastUpdateException => lastUpdateException?.Value;

        public ISolutionExecutorViewModel SolutionExecutor { get; }

        public ICommand WithdrawChaosSetCommand { get; }

        public ICommand WithdrawCurrencyCommand { get; }

        public ICommand WithdrawDivinationCardsCommand { get; }

        public ICommand WithdrawMapsCommand { get; }

        public ICommand WithdrawMiscCommand { get; }

        public ICommand WithdrawSellablesCommand { get; }

        public ICommand ForceRefreshCommand { get; }

        public IPoeStashUpdater StashUpdater
        {
            get => stashUpdater;
            set => this.RaiseAndSetIfChanged(ref stashUpdater, value);
        }

        public bool HideXpBar
        {
            get => hideXpBar;
            set => this.RaiseAndSetIfChanged(ref hideXpBar, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public StashViewModel Stash
        {
            get => stash;
            set => this.RaiseAndSetIfChanged(ref stash, value);
        }

        public string League => actualConfig?.LeagueId;

        public TimeSpan TimeTillNextUpdate
            =>
                stashUpdater == null || stashUpdater.LastUpdateTimestamp == DateTime.MinValue
                    ? TimeSpan.Zero
                    : stashUpdater.LastUpdateTimestamp + (actualConfig?.StashUpdatePeriod ?? TimeSpan.Zero) - clock.Now;

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();
            var config = configProvider.ActualConfig;
            SavePropertiesToConfig(config);

            configProvider.Save(config);
        }

        private void ApplyConfig(PoeBudConfig config)
        {
            Log.Debug($"[PoeBudViewModel] Applying new config...");

            actualConfig = config;
            IsEnabled = actualConfig.IsEnabled;
            hotkey = KeyGestureExtensions.SafeCreateGesture(config.GetChaosSetHotkey);
            RefreshStashUpdater(actualConfig);
            this.RaisePropertyChanged(nameof(League));

            base.ApplyConfig(config);
        }

        private void RefreshStashUpdater(PoeBudConfig config)
        {
            var stashDisposable = new CompositeDisposable();

            try
            {
                Log.Info($"[PoeBudViewModel] Reinitializing PoeBud...");
                stashUpdaterDisposable.Disposable = null;
                StashUpdater = null;
                Stash = null;
                lastServerStashUpdate = null;

                if (string.IsNullOrEmpty(config.LoginEmail) || string.IsNullOrEmpty(config.SessionId))
                {
                    Log.Warn($"[PoeBudViewModel] Credentials are not set, userName: {config.LoginEmail}, sessionId: {config.SessionId}");
                    return;
                }

                if (!config.IsEnabled)
                {
                    Log.Debug($"[PoeBudViewModel] PoeBud is disabled, terminating...");
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
                    .Subscribe(() => ExecuteChaosSolutionCommandExecuted().Wait())
                    .AddTo(stashDisposable);

                var forceRefreshHotkey = new KeyGesture(hotkey.Key, ModifierKeys.Control | ModifierKeys.Shift);
                keyPressedObservable
                    .Where(x => forceRefreshHotkey.MatchesHotkey(x))
                    .Do(x => x.Handled = true)
                    .ObserveOn(uiScheduler)
                    .Subscribe(ForceRefreshStashCommandExecuted)
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
                Log.Error(ex);
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
                Log.Debug($"[PoeBudViewModel] Duplicate update arrived, skipping update");
                return;
            }

            Log.Debug($"[PoeBudViewModel] Stash update arrived, tabs: {stashUpdate.Tabs.Count()}, items: {stashUpdate.Items.Count()}");

            lastServerStashUpdate = stashUpdate;
            Stash = stashUpdateFactory.Create(stashUpdate, config);
        }

        private void ForceRefreshStashCommandExecuted()
        {
            try
            {
                Log.Debug($"[PoeBudViewModel] Force refresh requested");
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

        private async Task ExecuteSolutionCommandExecuted(IPoeTradeSolution solutionToExecute)
        {
            try
            {
                if (SolutionExecutor.IsBusy)
                {
                    Log.Debug(
                        "[MainViewModel.ExecuteSolutionCommandExecuted] Solution executor is busy, ignoring request");
                    return;
                }

                await TryToExecuteSolution(solutionToExecute, actualConfig);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }

        private async Task ExecuteChaosSolutionCommandExecuted()
        {
            try
            {
                var solutionToExecute = stash?.ChaosSetSolutions.FirstOrDefault();
                if (solutionToExecute == null)
                {
                    SolutionExecutor.LogOperation("Failed to find a solution, not enough items ?");
                }
                else
                {
                    await ExecuteSolutionCommandExecuted(solutionToExecute);
                }
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }

        private async Task ExecuteSolutionOrFail(Func<IPoeTradeSolution> solutionSupplier, Func<string> failMessageSupplier)
        {
            try
            {
                var solutionToExecute = solutionSupplier();
                if (solutionToExecute == null)
                {
                    SolutionExecutor.LogOperation(failMessageSupplier());
                }
                else
                {
                    await ExecuteSolutionCommandExecuted(solutionToExecute);
                }
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                exceptionsToPropagate.OnNext(ex);
            }
        }


        private async Task<bool> TryToExecuteSolution(IPoeTradeSolution solutionToExecute, PoeBudConfig config)
        {
            var window = WindowManager.ActiveWindow;
            if (window == null)
            {
                SolutionExecutor.LogOperation("Path of Exile window is not active");
                return false;
            }

            var stashSnapshot = stash;
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
                Log.Warn(
                    $"[MainViewModel.Sell] Possible race condition, trying to resolve it...");
            }

            Log.Debug($"[MainViewModel.Sell] Solution executed successfully, preparing DIRTY stash update...");

            var dirtyStashUpdate = stash.StashUpdate.RemoveItems(executedSolution.Items);
            Stash = stashUpdateFactory.Create(dirtyStashUpdate, config);
        }
    }
}