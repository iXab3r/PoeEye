using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using PoeShared.UI;
using ReactiveUI;
using Unity.Attributes;

namespace PoeBud.Models
{
    internal sealed class PoeStashUpdater : DisposableReactiveObject, IPoeStashUpdater
    {
        private readonly IClock clock;
        private readonly IStashUpdaterParameters config;
        private readonly IPoeStashClient poeClient;
        private readonly TimeSpan recheckPeriodThrottling = TimeSpan.FromMilliseconds(1000);
        private readonly ISubject<Unit> refreshSubject = new Subject<Unit>();

        private readonly ISubject<Exception> updateExceptionsSubject = new ReplaySubject<Exception>(1);
        private readonly ISubject<StashUpdate> updatesSubject = new ReplaySubject<StashUpdate>(1);
        private bool isBusy;
        private DateTime lastUpdateTimestamp;
        private TimeSpan recheckPeriod;
        private IStashUpdaterStrategy stashUpdaterStrategy;

        public PoeStashUpdater(
            [NotNull] IStashUpdaterParameters config,
            [NotNull] IClock clock,
            [NotNull] IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(config, nameof(config));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(poeClientFactory, nameof(poeClientFactory));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            Guard.ArgumentNotNullOrEmpty(() => config.LoginEmail);
            Guard.ArgumentNotNullOrEmpty(() => config.SessionId);

            this.config = config;
            this.clock = clock;
            Log.Instance.DebugFormat(
                "Initializing client for {0}@{1}", config.LoginEmail, config.LeagueId);

            var credentials = new NetworkCredential(config.LoginEmail, config.SessionId);
            poeClient = poeClientFactory.Create(credentials, true);

            var periodObservable = this.ObservableForProperty(x => x.RecheckPeriod)
                                       .Select(x => x.Value)
                                       .DistinctUntilChanged()
                                       .Throttle(recheckPeriodThrottling)
                                       .Select(
                                           timeout => timeout == TimeSpan.Zero
                                               ? Observable.Never<Unit>()
                                               : Observable.Timer(DateTimeOffset.Now, timeout).ToUnit())
                                       .Switch()
                                       .Publish();

            var queryObservable = refreshSubject
                                  .Where(x => !IsBusy)
                                  .Do(StartUpdate)
                                  .Select(x => Observable.Start(Refresh, bgScheduler))
                                  .Switch()
                                  .Do(HandleUpdate, HandleUpdateError);

            Observable
                .Defer(() => queryObservable)
                .Retry()
                .Subscribe()
                .AddTo(Anchors);

            periodObservable.Subscribe(refreshSubject).AddTo(Anchors);
            periodObservable.Connect();
        }

        public TimeSpan RecheckPeriod
        {
            get => recheckPeriod;
            set => this.RaiseAndSetIfChanged(ref recheckPeriod, value);
        }

        public DateTime LastUpdateTimestamp
        {
            get => lastUpdateTimestamp;
            set => this.RaiseAndSetIfChanged(ref lastUpdateTimestamp, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public IObservable<StashUpdate> Updates => updatesSubject;

        public IObservable<Exception> UpdateExceptions => updateExceptionsSubject;

        public void ForceRefresh()
        {
            Log.Instance.Debug($"Force update requested");
            refreshSubject.OnNext(Unit.Default);
        }

        public void SetStrategy(IStashUpdaterStrategy strategy)
        {
            Guard.ArgumentNotNull(strategy, nameof(strategy));

            Log.Instance.Debug($"New strategy: {strategy}");
            stashUpdaterStrategy = strategy;
        }

        private StashUpdate Refresh(IStashUpdaterStrategy strategy)
        {
            Log.Instance.Debug($"Refreshing stash, strategy: {strategy}");

            var leaguesToProcess = strategy.GetDefaultLeaguesList();
            if (leaguesToProcess.Length == 0)
            {
                Log.Instance.Debug($"Strategy did not provide default leagues list");
                Log.Instance.Debug($"Requesting leagues list...");
                var leagues = poeClient.GetLeaguesAsync().Result;
                Log.Instance.Debug($"Received leagues list: {leagues.DumpToTextRaw()}");
                leaguesToProcess = strategy.GetLeaguesToProcess(leagues);
                Log.Instance.Debug($"Leagues to process: {leaguesToProcess.DumpToTextRaw()}");
            }

            var allStashes = new List<IStash>();

            foreach (var league in leaguesToProcess)
            {
                var stashesToRequest = new IStashTab[0];
                try
                {
                    Log.Instance.Debug($"[League {league.Id}] Requesting stash #0...");
                    var zeroStash = poeClient.GetStash(0, league.Id);

                    var tabs = zeroStash.Tabs?.ToArray() ?? new IStashTab[0];

                    if (tabs.Length == 0)
                    {
                        Log.Instance.Debug($"No stashes found in league {league.Id}");
                        continue;
                    }

                    Log.Instance.Debug($"Tabs({tabs.Length}) in league {league.Id}: {tabs.Select(x => x.Name).DumpToTextRaw()}");
                    stashesToRequest = strategy.GetTabsToProcess(tabs);
                }
                catch (Exception e)
                {
                    Log.Instance.Warn($"Failed to request tabs in league {league.DumpToTextRaw()}", e);
                }

                if (stashesToRequest.Length == 0)
                {
                    Log.Instance.Debug($"No stashes to process in league {league.Id}");
                    continue;
                }

                Log.Instance.Debug($"Requesting stashes [{stashesToRequest.DumpToTextRaw()}]...");
                foreach (var tab in stashesToRequest)
                {
                    try
                    {
                        Log.Instance.Debug($"[League {league.Id}] Requesting stash tab [{tab.DumpToTextRaw()}]...");
                        var stash = poeClient.GetStash(tab.Idx, league.Id);
                        Log.Instance.Debug($"[League {league.Id}] Result: {stash.Items.EmptyIfNull().Count()} item(s), stash tab [{tab.DumpToTextRaw()}]");

                        allStashes.Add(stash);
                    }
                    catch (Exception e)
                    {
                        Log.Instance.Warn($"Failed to request tab '{tab.Name}'(Idx: {tab.Idx}), league {league.DumpToTextRaw()}", e);
                    }
                }
            }

            var allItems = allStashes
                           .SelectMany(x => x.Items)
                           .Distinct(new LambdaComparer<IStashItem>((x, y) => x?.Id == y?.Id))
                           .ToArray();
            var allTabs = allStashes
                          .SelectMany(x => x.Tabs)
                          .Distinct(new LambdaComparer<IStashTab>((x, y) => x?.Id == y?.Id))
                          .ToArray();

            Log.Instance.Debug($"Got {allItems.Length} item(s) from {allTabs.Length} tab(s)...");
            return new StashUpdate(allItems, allTabs.ToArray());
        }

        private StashUpdate Refresh()
        {
            Log.Instance.Debug($"Updating stash(period:{recheckPeriod.TotalSeconds:F0}s)...");
            if (!poeClient.IsAuthenticated)
            {
                Log.Instance.Debug("Authenticating...");
                poeClient.Authenticate();
            }

            Thread.Sleep((int)UiConstants.ArtificialLongDelay.TotalMilliseconds);


            var strategy = stashUpdaterStrategy;
            if (strategy == null)
            {
                Log.Instance.Warn($"Strategy is not set");
                return StashUpdate.Empty;
            }

            return Refresh(strategy);
        }

        private void StartUpdate(Unit unit)
        {
            IsBusy = true;
        }

        private void HandleUpdate(StashUpdate update)
        {
            Log.Instance.Debug($"Stash update received, tabs: {update.Tabs.Count()}, items: {update.Items.Count()}");

            IsBusy = false;
            LastUpdateTimestamp = clock.Now;
            updateExceptionsSubject.OnNext(null);
            updatesSubject.OnNext(update);
        }

        private void HandleUpdateError(Exception ex)
        {
            Guard.ArgumentNotNull(ex, nameof(ex));

            Log.HandleException(ex);
            LastUpdateTimestamp = clock.Now;
            IsBusy = false;
            updateExceptionsSubject.OnNext(ex);
        }
    }
}