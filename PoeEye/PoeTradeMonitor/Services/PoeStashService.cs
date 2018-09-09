using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Scaffolding;
using PoeEye.TradeMonitor.Modularity;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Services
{
    internal sealed class PoeStashService : DisposableReactiveObject, IPoeStashService
    {
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IClock clock;
        private readonly IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory;
        [NotNull] private readonly IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory;
        private readonly BehaviorSubject<StashUpdate> stashUpdates = new BehaviorSubject<StashUpdate>(null);

        private IPoeStashUpdater activeUpdater;

        public PoeStashService(
            [NotNull] IClock clock,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> tradeMonitorConfigProvider,
            [NotNull] IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory,
            [NotNull] IFactory<IDefaultStashUpdaterStrategy, IStashUpdaterParameters> stashUpdaterStrategyFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(poeBudConfigProvider, nameof(poeBudConfigProvider));
            Guard.ArgumentNotNull(tradeMonitorConfigProvider, nameof(tradeMonitorConfigProvider));
            Guard.ArgumentNotNull(stashUpdaterStrategyFactory, nameof(stashUpdaterStrategyFactory));
            Guard.ArgumentNotNull(stashUpdaterFactory, nameof(stashUpdaterFactory));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.clock = clock;
            this.stashUpdaterFactory = stashUpdaterFactory;
            this.stashUpdaterStrategyFactory = stashUpdaterStrategyFactory;

            Observable.CombineLatest(
                    tradeMonitorConfigProvider.WhenChanged, 
                    poeBudConfigProvider.WhenChanged, 
                    (tradeMonitorConfig, poeBudConfig) => new {PoeBudConfig = poeBudConfig, TradeMonitorConfig = tradeMonitorConfig})
                .Subscribe(x => ApplyConfig(x.PoeBudConfig, x.TradeMonitorConfig))
                .AddTo(Anchors);
        }

        public IStashItem TryToFindItem(string tabName, int itemX, int itemY)
        {
            var stash = stashUpdates.Value;
            Log.Instance.Debug($"Trying to find item in tab {tabName} @ X{itemX} Y{itemY} (tabs count: {stash?.Tabs?.Length ?? 0})");
            if (stash == null || stash == StashUpdate.Empty)
            {
                Log.Instance.Warn($"Stash is not ready - either not requested yet or is empty");
                return null;
            }

            var matchingTab = TryToFindTab(tabName);
            if (matchingTab == null)
            {
                Log.Instance.Warn($"Failed to find tab {tabName}, tabs list: {stash.Tabs?.DumpToTextRaw()}");
                return null;
            }

            var inventoryId = matchingTab.GetInventoryId();
            var itemsInTab = stash.Items
                .Where(x => x.InventoryId == inventoryId)
                .Select(x => new { Position = new Rectangle(x.X, x.Y, x.Width, x.Height), Item = x })
                .ToArray();
            var requestedRect = new Rectangle(itemX, itemY, 1 ,1);
            Log.Instance.Debug($"Looking up item @ {requestedRect}, total items in tab '{matchingTab.Name}': {itemsInTab.Length}");
            var itemsAtThisPosition = itemsInTab
                .Where(x => x.Position.IntersectsWith(requestedRect))
                .Select(x => x.Item)
                .ToArray();
            Log.Instance.Debug($"Got {itemsAtThisPosition.Length} item(s) @ {requestedRect}");
            return itemsAtThisPosition.FirstOrDefault();
        }

        public IStashTab TryToFindTab(string tabName)
        {
            var stash = stashUpdates.Value;
            Log.Instance.Debug($"Trying to find item int tab {tabName}");
            if (stash == null || stash == StashUpdate.Empty)
            {
                Log.Instance.Warn($"Stash is not ready - either not requested yet or is empty");
                return null;
            }
            var matchingTab = stash.Tabs.FirstOrDefault(x => x.Name == tabName);
            return matchingTab;
        }

        public IObservable<Unit> Updates => stashUpdates.ToUnit();

        public bool IsBusy => activeUpdater?.IsBusy ?? false;

        public DateTime LastUpdateTimestamp => activeUpdater?.LastUpdateTimestamp ?? DateTime.MinValue;

        private void ApplyConfig(IPoeBudConfig poeBudConfig, PoeTradeMonitorConfig tradeMonitorConfig)
        {
            RefreshStashUpdater(poeBudConfig, tradeMonitorConfig);
        }

        private void RefreshStashUpdater(IPoeBudConfig config, PoeTradeMonitorConfig tradeMonitorConfig)
        {
            var stashDisposable = new CompositeDisposable();

            try
            {
                Log.Instance.Info($"[TradeMonitor.PoeStashService] Reinitializing stash service...");
                stashUpdaterDisposable.Disposable = null;
                stashUpdates.OnNext(StashUpdate.Empty);
                activeUpdater = null;

                if (string.IsNullOrEmpty(config.LoginEmail) || string.IsNullOrEmpty(config.SessionId))
                {
                    Log.Instance.Warn(
                        $"[TradeMonitor.PoeStashService] Credentials are not set, userName: {config.LoginEmail}, sessionId: {config.SessionId}");
                    return;
                }

                if (!tradeMonitorConfig.IsEnabled)
                {
                    Log.Instance.Debug($"[TradeMonitor.PoeStashService] Service is disabled, terminating...");
                    return;
                }

                var parameters = new StashUpdaterParameters(config);
                var strategy = stashUpdaterStrategyFactory.Create(parameters);

                var updater = stashUpdaterFactory.Create(parameters);
                stashDisposable.Add(updater);

                updater.SetStrategy(strategy);

                updater
                    .Updates
                    .Do(x => Log.Instance.Debug($"[TradeMonitor.PoeStashService] Got {x.Items?.Length} item(s) and {x.Tabs?.Length} tabs"))
                    .Subscribe(stashUpdates)
                    .AddTo(stashDisposable);

                updater.RecheckPeriod = config.StashUpdatePeriod;
                activeUpdater = updater;

                this.BindPropertyTo(x => x.LastUpdateTimestamp, updater, x => x.LastUpdateTimestamp).AddTo(stashDisposable);
                this.BindPropertyTo(x => x.IsBusy, updater, x => x.IsBusy).AddTo(stashDisposable);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"Failed to initialize stash updater using config:\n{config.DumpToText()}", ex);
            }
            finally
            {
                stashUpdaterDisposable.Disposable = stashDisposable;
            }
        }

        private sealed class StashUpdaterParameters : IStashUpdaterParameters
        {
            private readonly IPoeBudConfig config;
            public StashUpdaterParameters(IPoeBudConfig config)
            {
                this.config = config;
            }

            public string LoginEmail => config.LoginEmail;
            public string SessionId => config.SessionId;
            public string LeagueId => config.LeagueId;
            public string[] StashesToProcess { get; } = new string[0];
        }
    }
}
