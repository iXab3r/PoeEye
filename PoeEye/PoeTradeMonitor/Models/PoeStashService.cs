using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Scaffolding;
using PoeEye.TradeMonitor.Modularity;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class PoeStashService : DisposableReactiveObject, IPoeStashService
    {
        private readonly IScheduler bgScheduler;
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory;
        private readonly BehaviorSubject<StashUpdate> stashUpdates = new BehaviorSubject<StashUpdate>(null);
        private readonly IScheduler uiScheduler;

        private IPoeStashUpdater activeUpdater;

        public PoeStashService(
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> tradeMonitorConfigProvider,
            [NotNull] IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => poeBudConfigProvider);
            Guard.ArgumentNotNull(() => tradeMonitorConfigProvider);
            Guard.ArgumentNotNull(() => stashUpdaterFactory);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.stashUpdaterFactory = stashUpdaterFactory;
            this.uiScheduler = uiScheduler;
            this.bgScheduler = bgScheduler;

            tradeMonitorConfigProvider.WhenAnyValue(x => x.ActualConfig)
                .CombineLatest(
                    poeBudConfigProvider.WhenAnyValue(x => x.ActualConfig),
                    (tradeMonitorConfig, poeBudConfig) =>
                        new {PoeBudConfig = poeBudConfig, TradeMonitorConfig = tradeMonitorConfig})
                .Subscribe(x => ApplyConfig(x.PoeBudConfig, x.TradeMonitorConfig))
                .AddTo(Anchors);
        }

        public IStashItem TryToFindItem(string tabName, int itemX, int itemY)
        {
            var stash = stashUpdates.Value;
            if (stash == null || stash == StashUpdate.Empty)
            {
                return null;
            }

            var matchingTab = stash.Tabs.FirstOrDefault(x => x.Name == tabName);
            if (matchingTab == null)
            {
                return null;
            }
            var inventoryId = matchingTab.GetInventoryId();
            var itemsInTab = stash.Items.Where(x => x.InventoryId == inventoryId).ToArray();

            var itemsAtThisPosition = itemsInTab
                .Where(x => x.X == itemX - 1 && x.Y == itemY - 1)
                .ToArray();
            return itemsAtThisPosition.FirstOrDefault();
        }

        public IObservable<Unit> Updates => stashUpdates.ToUnit();

        public DateTime LastUpdateTimestamp => activeUpdater.LastUpdateTimestamp;

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
                var updater = stashUpdaterFactory.Create(parameters);
                stashDisposable.Add(updater);

                updater
                    .Updates
                    .ObserveOn(uiScheduler)
                    .Do(x => Log.Instance.Debug($"[TradeMonitor.PoeStashService] Got {x.Items?.Length} item(s) and {x.Tabs?.Length} tabs"))
                    .Subscribe(stashUpdates)
                    .AddTo(stashDisposable);

                updater
                    .WhenAnyValue(x => x.LastUpdateTimestamp)
                    .ObserveOn(uiScheduler)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(LastUpdateTimestamp)))
                    .AddTo(stashDisposable);

                updater.RecheckPeriod = config.StashUpdatePeriod;
                activeUpdater = updater;
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
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
            public string CharacterName => config.CharacterName;
            public ICollection<int> StashesToProcess { get; } = new List<int>();
        }
    }
}