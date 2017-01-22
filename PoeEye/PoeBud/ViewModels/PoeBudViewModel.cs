using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsInput.Native;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.OfficialApi.DataTypes;
using PoeBud.Utilities;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;
using WinFormsKeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeBud.ViewModels
{
    internal sealed class PoeBudViewModel : DisposableReactiveObject, IOverlayViewModel
    {
        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(1);
        private readonly IClock clock;
        private readonly IKeyboardMouseEvents keyboardMouseEvents;
        private readonly ISubject<Exception> exceptionsToPropagate = new Subject<Exception>();
        private readonly ObservableAsPropertyHelper<Exception> lastUpdateException;
        private readonly IFactory<PoeStashUpdater, IPoeBudConfig> stashAnalyzerFactory;
        private readonly IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory;
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IScheduler uiScheduler;

        private PoeBudConfig actualConfig;
        private bool hideXpBar;
        private KeyGesture hotkey;
        private bool isEnabled;
        private StashUpdate lastServerStashUpdate;
        private StashViewModel stash;
        private PoeStashUpdater stashUpdater;

        public PoeBudViewModel(
            [NotNull] IPoeWindowManager windowManager,
            [NotNull] ISolutionExecutorViewModel solutionExecutor,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] IClock clock,
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] IUserInteractionsManager userInteractionsManager,
            [NotNull] IFactory<PoeStashUpdater, IPoeBudConfig> stashAnalyzerFactory,
            [NotNull] IFactory<StashViewModel, StashUpdate, IPoeBudConfig> stashUpdateFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => windowManager);
            Guard.ArgumentNotNull(() => solutionExecutor);
            Guard.ArgumentNotNull(() => userInteractionsManager);
            Guard.ArgumentNotNull(() => poeBudConfigProvider);
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => stashAnalyzerFactory);
            Guard.ArgumentNotNull(() => stashUpdateFactory);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.clock = clock;
            this.keyboardMouseEvents = keyboardMouseEvents;
            this.stashAnalyzerFactory = stashAnalyzerFactory;
            this.stashUpdateFactory = stashUpdateFactory;
            this.uiScheduler = uiScheduler;

            SolutionExecutor = solutionExecutor;
            WindowManager = windowManager;

            poeBudConfigProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            exceptionsToPropagate
                .ToProperty(this, x => x.LastUpdateException, out lastUpdateException, null, uiScheduler)
                .AddTo(Anchors);
        }

        public IPoeWindowManager WindowManager { get; }

        public Exception LastUpdateException => lastUpdateException?.Value;

        public ISolutionExecutorViewModel SolutionExecutor { get; }

        public PoeStashUpdater StashUpdater
        {
            get { return stashUpdater; }
            set { this.RaiseAndSetIfChanged(ref stashUpdater, value); }
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

        public TimeSpan TimeTillNextUpdate
            =>
                stashUpdater == null || stashUpdater.LastUpdateTimestamp == DateTime.MinValue
                    ? TimeSpan.Zero
                    : stashUpdater.LastUpdateTimestamp + (actualConfig?.StashUpdatePeriod ?? TimeSpan.Zero) - clock.Now;

        public Point Location { get; } = new Point();

        public Size Size { get; } = new Size(double.NaN, double.NaN);

        private void ApplyConfig(PoeBudConfig config)
        {
            actualConfig = config;
            HideXpBar = config.HideXpBar;
            IsEnabled = actualConfig.IsEnabled;
            hotkey = new KeyGestureConverter().ConvertFromInvariantString(config.GetSetHotkey) as KeyGesture;
            RefreshStashUpdater(actualConfig);
        }

        private void RefreshStashUpdater(PoeBudConfig config)
        {
            var stashDisposable = new CompositeDisposable();

            try
            {
                stashUpdaterDisposable.Disposable = null;
                StashUpdater = null;
                Stash = null;
                lastServerStashUpdate = null;

                if (string.IsNullOrEmpty(config.LoginEmail) || string.IsNullOrEmpty(config.SessionId))
                {
                    throw new UnauthorizedAccessException(
                        $"Credentials are not set, userName: {config.LoginEmail}, sessionId: {config.SessionId}");
                }

                if (!config.IsEnabled)
                {
                    return;
                }

                Observable.FromEventPattern<WinFormsKeyEventHandler, WinFormsKeyEventArgs>(
                        h => keyboardMouseEvents.KeyDown += h,
                        h => keyboardMouseEvents.KeyDown -= h)
                    .Where(x => IsEnabled)
                    .Where(x => !SolutionExecutor.IsBusy)
                    .Where(x => WindowManager.ActiveWindow != null)
                    .Where(x => hotkey.MatchesHotkey(x.EventArgs))
                    .Do(x => x.EventArgs.Handled = true)
                    .Subscribe(ExecuteSolutionCommandExecuted)
                    .AddTo(stashDisposable);

                var updater = stashAnalyzerFactory.Create(config);
                stashDisposable.Add(updater);

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
            Log.Instance.Debug($"[MainViewModel] Update arrived");

            lastServerStashUpdate = stashUpdate;
            Stash = stashUpdateFactory.Create(stashUpdate, config);
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

            var solutionToExecute = stashSnapshot?.Solutions.FirstOrDefault();
            if (solutionToExecute == null)
            {
                return false;
            }

            var window = WindowManager.ActiveWindow;
            if (window == null)
            {
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

        private bool IsMatch(IPoeTradeItem tradeItem, IItem item)
        {
            return tradeItem.TabIndex == item.GetTabIndex() && tradeItem.X == item.X && tradeItem.Y == item.Y;
        }
    }
}