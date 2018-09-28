using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeShared.PoeTrade
{
    internal sealed class PoeLiveHistoryProvider : DisposableReactiveObject, IPoeLiveHistoryProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeLiveHistoryProvider));

        private readonly TimeSpan delayAfterError = TimeSpan.FromSeconds(30);

        private readonly HashSet<IPoeItem> existingItems = new HashSet<IPoeItem>();

        private readonly ISubject<Unit> forceUpdatesSubject = new Subject<Unit>();

        private bool isBusy;
        private TimeSpan recheckPeriod;
        private Exception lastException;
        private IPoeItem[] itemPack;
        private IPoeQueryResult queryResult;

        public PoeLiveHistoryProvider(
            [NotNull] IPoeQueryInfo query,
            [NotNull] IPoeApiWrapper poeApi,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(query, nameof(query));
            Guard.ArgumentNotNull(poeApi, nameof(poeApi));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            var periodObservable = this.WhenAnyValue(x => x.RecheckPeriod)
                                       .Do(_ => LogRecheckPeriodChange())
                                       .Select(_ => ToTimer())
                                       .Switch()
                                       .Publish();

            var queryObservable = Observable.Merge(periodObservable, forceUpdatesSubject)
                                            .Where(x => !IsBusy)
                                            .Do(StartUpdate)
                                            .ObserveOn(bgScheduler)
                                            .Select(x =>
                                            {
                                                return IsLiveMode
                                                    ? new PoeLiveUpdatesAdapter(poeApi).SubscribeToLiveUpdates(query)
                                                    : poeApi.IssueQuery(query)
                                                            .ToObservable();
                                            })
                                            .ObserveOn(uiScheduler)
                                            .Switch()
                                            .Select(ProcessPacks)
                                            .Do(HandleUpdate, HandleUpdateError);

            Observable
                .Defer(() => queryObservable)
                .RetryWithDelay(delayAfterError)
                .Subscribe()
                .AddTo(Anchors);

            periodObservable.Connect().AddTo(Anchors);

            Disposable.Create(() => poeApi.DisposeQuery(query)).AddTo(Anchors);
        }

        public bool IsLiveMode => RecheckPeriod == TimeSpan.Zero;

        public bool IsAutoRecheckEnabled => RecheckPeriod > TimeSpan.Zero;

        public Exception LastException
        {
            get => lastException;
            private set => this.RaiseAndSetIfChanged(ref lastException, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public TimeSpan RecheckPeriod
        {
            get => recheckPeriod;
            set => this.RaiseAndSetIfChanged(ref recheckPeriod, value);
        }

        public IPoeItem[] ItemPack
        {
            get => itemPack;
            private set => this.RaiseAndSetIfChanged(ref itemPack, value);
        }

        public IPoeQueryResult QueryResult
        {
            get => queryResult;
            set => this.RaiseAndSetIfChanged(ref queryResult, value);
        }

        public void Refresh()
        {
            forceUpdatesSubject.OnNext(Unit.Default); // forcing refresh
        }

        private IObservable<Unit> ToTimer()
        {
            if (IsLiveMode)
            {
                return Observable.Never<Unit>();
            }

            if (IsAutoRecheckEnabled)
            {
                return Observable.Timer(DateTimeOffset.Now, RecheckPeriod).ToUnit();
            }

            return Observable.Return(Unit.Default).Concat(Observable.Never<Unit>());
        }

        private void StartUpdate(Unit unit)
        {
            Log.Debug("[PoeLiveHistoryProvider] Starting update...");
            if (!IsLiveMode)
            {
                IsBusy = true;
            }
        }

        private void LogRecheckPeriodChange()
        {
            string periodInfo;
            if (IsLiveMode)
            {
                periodInfo = "Live mode";
            }
            else if (IsAutoRecheckEnabled)
            {
                periodInfo = $"every {RecheckPeriod}";
            }
            else
            {
                periodInfo = "recheck disabled";
            }

            Log.Debug($"[PoeLiveHistoryProvider] Update period changed: {periodInfo}");
        }

        private IPoeQueryResult ProcessPacks(IPoeQueryResult queryResult)
        {
            Guard.ArgumentNotNull(queryResult, nameof(queryResult));
            var updated = queryResult.ItemsList.EmptyIfNull().ToArray();
            var result = new List<IPoeItem>(updated.Length);
            result.AddRange(updated.Where(x => x.ItemState != PoeTradeState.Unknown));

            var updatedUnknown = updated.Where(x => x.ItemState == PoeTradeState.Unknown).ToArray();
            if (updatedUnknown.Any())
            {
                var removedItems = existingItems.Except(updatedUnknown, PoeItemEqualityComparer.Instance).ForEach(x => x.ItemState = PoeTradeState.Removed);
                var newItems = updatedUnknown.Except(existingItems, PoeItemEqualityComparer.Instance).ForEach(x => x.ItemState = PoeTradeState.New);
                result.AddRange(removedItems);
                result.AddRange(newItems);
            }

            foreach (var item in result)
            {
                switch (item.ItemState)
                {
                    case PoeTradeState.New:
                        existingItems.Add(item);
                        break;
                    case PoeTradeState.Removed:
                        existingItems.Remove(item);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Invalid item state: {item.ItemState}, expected to be New/Removed at this stage, items: {result.DumpToTextRaw()}");
                }
            }

            return new PoeQueryResult(queryResult)
            {
                ItemsList = result.ToArray(),
            };
        }

        private void HandleUpdate(IPoeQueryResult queryResult)
        {
            Guard.ArgumentNotNull(queryResult, nameof(queryResult));

            Log.Debug($"[PoeLiveHistoryProvider] Update received, itemsCount: {queryResult.ItemsList.Length}");
            IsBusy = false;
            LastException = null;
            ItemPack = queryResult.ItemsList;
            QueryResult = queryResult;
        }

        private void HandleUpdateError(Exception ex)
        {
            Guard.ArgumentNotNull(ex, nameof(ex));

            Log.Error("[PoeLiveHistoryProvider] Update failed", ex);
            IsBusy = false;
            LastException = ex;
        }

        private sealed class PoeLiveUpdatesAdapter : DisposableReactiveObject
        {
            private readonly IPoeApiWrapper api;

            public PoeLiveUpdatesAdapter([NotNull] IPoeApiWrapper api)
            {
                Guard.ArgumentNotNull(api, nameof(api));

                this.api = api;
            }

            public IObservable<IPoeQueryResult> SubscribeToLiveUpdates([NotNull] IPoeQueryInfo query)
            {
                Guard.ArgumentNotNull(query, nameof(query));

                return api
                       .GetLiveUpdates(query);
            }
        }
    }
}