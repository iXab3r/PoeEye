﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using Anotar.Log4Net;
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
using PoeShared.StashApi.DataTypes;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Services
{
    internal sealed class PoeStashService : DisposableReactiveObject, IPoeStashService
    {
        private readonly SerialDisposable stashUpdaterDisposable = new SerialDisposable();
        private readonly IClock clock;
        private readonly IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory;
        private readonly BehaviorSubject<StashUpdate> stashUpdates = new BehaviorSubject<StashUpdate>(null);

        private IPoeStashUpdater activeUpdater;

        public PoeStashService(
            [NotNull] IClock clock,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> tradeMonitorConfigProvider,
            [NotNull] IFactory<IPoeStashUpdater, IStashUpdaterParameters> stashUpdaterFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(poeBudConfigProvider, nameof(poeBudConfigProvider));
            Guard.ArgumentNotNull(tradeMonitorConfigProvider, nameof(tradeMonitorConfigProvider));
            Guard.ArgumentNotNull(stashUpdaterFactory, nameof(stashUpdaterFactory));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.clock = clock;
            this.stashUpdaterFactory = stashUpdaterFactory;

            Observable.CombineLatest(
                    tradeMonitorConfigProvider.WhenChanged, 
                    poeBudConfigProvider.WhenAnyValue(x => x.ActualConfig), 
                    (tradeMonitorConfig, poeBudConfig) => new {PoeBudConfig = poeBudConfig, TradeMonitorConfig = tradeMonitorConfig})
                .Subscribe(x => ApplyConfig(x.PoeBudConfig, x.TradeMonitorConfig))
                .AddTo(Anchors);
        }

        public IStashItem TryToFindItem(string tabName, int itemX, int itemY)
        {
            var stash = stashUpdates.Value;
            LogTo.Debug($"Trying to find item in tab {tabName} @ X{itemX} Y{itemY} (tabs count: {stash?.Tabs?.Length ?? 0})");
            if (stash == null || stash == StashUpdate.Empty)
            {
                LogTo.Warn($"Stash is not ready - either not requested yet or is empty");
                return null;
            }

            var matchingTab = TryToFindTab(tabName);
            if (matchingTab == null)
            {
                LogTo.Warn($"Failed to find tab {tabName}, tabs list: {stash.Tabs?.DumpToTextRaw()}");
                return null;
            }

            var inventoryId = matchingTab.GetInventoryId();
            var itemsInTab = stash.Items
                .Where(x => x.InventoryId == inventoryId)
                .Select(x => new { Position = new Rectangle(x.X, x.Y, x.Width, x.Height), Item = x })
                .ToArray();
            var requestedRect = new Rectangle(itemX, itemY, 1 ,1);
            LogTo.Debug($"Looking up item @ {requestedRect}, total items in tab '{matchingTab.Name}': {itemsInTab.Length}");
            var itemsAtThisPosition = itemsInTab
                .Where(x => x.Position.IntersectsWith(requestedRect))
                .Select(x => x.Item)
                .ToArray();
            LogTo.Debug($"Got {itemsAtThisPosition.Length} item(s) @ {requestedRect}");
            return itemsAtThisPosition.FirstOrDefault();
        }

        public IStashTab TryToFindTab(string tabName)
        {
            var stash = stashUpdates.Value;
            LogTo.Debug($"Trying to find item int tab {tabName}");
            if (stash == null || stash == StashUpdate.Empty)
            {
                LogTo.Warn($"Stash is not ready - either not requested yet or is empty");
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
                LogTo.Info($"[TradeMonitor.PoeStashService] Reinitializing stash service...");
                stashUpdaterDisposable.Disposable = null;
                stashUpdates.OnNext(StashUpdate.Empty);
                activeUpdater = null;

                if (string.IsNullOrEmpty(config.LoginEmail) || string.IsNullOrEmpty(config.SessionId))
                {
                    LogTo.Warn(
                        $"[TradeMonitor.PoeStashService] Credentials are not set, userName: {config.LoginEmail}, sessionId: {config.SessionId}");
                    return;
                }

                if (!tradeMonitorConfig.IsEnabled)
                {
                    LogTo.Debug($"[TradeMonitor.PoeStashService] Service is disabled, terminating...");
                    return;
                }

                var parameters = new StashUpdaterParameters(config);
                var strategy = new UpdaterStrategy(clock);

                var updater = stashUpdaterFactory.Create(parameters);
                stashDisposable.Add(updater);

                updater.SetStrategy(strategy);

                updater
                    .Updates
                    .Do(x => LogTo.Debug($"[TradeMonitor.PoeStashService] Got {x.Items?.Length} item(s) and {x.Tabs?.Length} tabs"))
                    .Subscribe(stashUpdates)
                    .AddTo(stashDisposable);

                updater.RecheckPeriod = config.StashUpdatePeriod;
                activeUpdater = updater;

                this.BindPropertyTo(x => x.LastUpdateTimestamp, updater, x => x.LastUpdateTimestamp).AddTo(stashDisposable);
                this.BindPropertyTo(x => x.IsBusy, updater, x => x.IsBusy).AddTo(stashDisposable);
            }
            catch (Exception ex)
            {
                LogTo.WarnException($"Failed to initialize stash updater using config:\n{config.DumpToText()}", ex);
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

        private sealed class UpdaterStrategy : IStashUpdaterStrategy
        {
            private readonly IClock clock;
            private ILeague[] leaguesToProcess;

            public UpdaterStrategy(IClock clock)
            {
                this.clock = clock;
            }

            public IStashTab[] GetTabsToProcess(IEnumerable<IStashTab> tabs)
            {
                Guard.ArgumentNotNull(tabs, nameof(tabs));

                var publicTabs = tabs
                    .Where(x => !x.Hidden)
                    .ToArray();
                LogTo.Debug($"Public tabs list: {publicTabs.DumpToText()}");
                return publicTabs;
            }

            public ILeague[] GetLeaguesToProcess(IEnumerable<ILeague> leagues)
            {
                leaguesToProcess = leagues
                    .Where(x => x.StartAt <= clock.Now && x.EndAt >= clock.Now)
                    .ToArray();
                LogTo.Debug($"Leagues to process: {leaguesToProcess.DumpToText()}");
                return leaguesToProcess.ToArray();
            }

            public ILeague[] GetDefaultLeaguesList()
            {
                return leaguesToProcess ?? new ILeague[0];
            }
        }
    }
}
