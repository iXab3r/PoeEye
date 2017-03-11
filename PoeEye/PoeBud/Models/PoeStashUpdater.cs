using System.Threading;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Prism;

    using ReactiveUI;

    internal sealed class PoeStashUpdater : DisposableReactiveObject
    {
        private readonly IPoeBudConfig config;
        [NotNull] private readonly IClock clock;
        private readonly TimeSpan recheckPeriodThrottling = TimeSpan.FromMilliseconds(1000);

        private readonly ISubject<Exception> updateExceptionsSubject = new ReplaySubject<Exception>(1);
        private readonly ISubject<StashUpdate> updatesSubject = new ReplaySubject<StashUpdate>(1);
        private readonly ISubject<Unit> refreshSubject = new Subject<Unit>();
        private readonly IPoeStashClient poeClient;
        private TimeSpan recheckPeriod;
        private DateTime lastUpdateTimestamp;
        private bool isBusy;

        public PoeStashUpdater(
                [NotNull] IPoeBudConfig config,
                [NotNull] IClock clock,
                [NotNull] IFactory<IPoeStashClient, NetworkCredential, bool> poeClientFactory,
                [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
                [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => config);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => poeClientFactory);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => uiScheduler);

            Guard.ArgumentNotNullOrEmpty(() => config.LoginEmail);
            Guard.ArgumentNotNullOrEmpty(() => config.SessionId);

            this.config = config;
            this.clock = clock;
            Log.Instance.DebugFormat("[PoeStashUpdater] Initializing client for {0}@{1}", config.LoginEmail, config.CharacterName);

            var credentials = new NetworkCredential(config.LoginEmail, config.SessionId);
            poeClient = poeClientFactory.Create(credentials, true);

            var periodObservable = this.ObservableForProperty(x => x.RecheckPeriod)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .Throttle(recheckPeriodThrottling)
                .Select(timeout => timeout == TimeSpan.Zero
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
            get { return recheckPeriod; }
            set { this.RaiseAndSetIfChanged(ref recheckPeriod, value); }
        }

        public DateTime LastUpdateTimestamp
        {
            get { return lastUpdateTimestamp; }
            set { this.RaiseAndSetIfChanged(ref lastUpdateTimestamp, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public IObservable<StashUpdate> Updates => updatesSubject;

        public IObservable<Exception> UpdateExceptions => updateExceptionsSubject;

        public void ForceRefresh()
        {
            Log.Instance.Debug($"[PoeStashUpdater.ForceRefresh] Force update requested");
            refreshSubject.OnNext(Unit.Default);
        }

        private StashUpdate Refresh()
        {
            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Updating stash(period:{recheckPeriod.TotalSeconds:F0}s)...");
            if (!poeClient.IsAuthenticated)
            {
                Log.Instance.Debug("[PoeStashUpdater.Refresh] Authenticating...");
                poeClient.Authenticate();
            }
            Thread.Sleep(5000);

            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Requesting characters list...");
            var charactersList = poeClient.GetCharacters();
            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Characters list: {charactersList.DumpToText()}");

            var character = charactersList.SingleOrDefault(x => x.Name == config.CharacterName);
            if (character == null)
            {
                throw new ApplicationException($"Could not find character '{config.CharacterName}' among the following:\r\n\t{string.Join<ICharacter>("\r\n\t", charactersList)}");
            }

            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Requesting stash #0...");
            var zeroStash = poeClient.GetStash(0, character.League);
            var tabs = zeroStash.Tabs?.ToArray() ?? new IStashTab[0];
            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Tabs({tabs.Length}): {tabs.Select(x => x.Name).DumpToText()}");

            var stashesToRequest = config.StashesToProcess
                .Where(x => x >= 0 && x < tabs.Length)
                .ToArray();

            if (!stashesToRequest.SequenceEqual(config.StashesToProcess))
            {
                Log.Instance.Warn($"[PoeStashUpdater.Refresh] Not all tabs from config will be requested, config: {config.StashesToProcess.DumpToText()}, toRequest: {stashesToRequest.DumpToText()}");
            }

            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Requesting stashes [{stashesToRequest.DumpToText()}]...");
            var allStashes = stashesToRequest
                .Select(tabIdx => poeClient.GetStash(tabIdx, character.League))
                .ToArray();

            var allItems = allStashes.SelectMany(x => x.Items).ToArray();
            Log.Instance.Debug($"[PoeStashUpdater.Refresh] Got {allItems.Length} item(s)...");
            return new StashUpdate(allItems, tabs);
        }

        private void StartUpdate(Unit unit)
        {
            IsBusy = true;
        }

        private void HandleUpdate(StashUpdate update)
        {
            IsBusy = false;
            LastUpdateTimestamp = clock.Now;
            updateExceptionsSubject.OnNext(null);
            updatesSubject.OnNext(update);
        }

        private void HandleUpdateError(Exception ex)
        {
            Guard.ArgumentNotNull(() => ex);

            LastUpdateTimestamp = clock.Now;
            IsBusy = false;
            updateExceptionsSubject.OnNext(ex);
        }
    }
}