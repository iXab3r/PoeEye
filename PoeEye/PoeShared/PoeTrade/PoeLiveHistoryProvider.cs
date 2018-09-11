using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.PoeTrade
{
    internal sealed class PoeLiveHistoryProvider : DisposableReactiveObject, IPoeLiveHistoryProvider
    {
        private readonly TimeSpan delayAfterError = TimeSpan.FromSeconds(30);
        
        private readonly ISubject<Unit> forceUpdatesSubject = new Subject<Unit>();
        private readonly ISubject<IPoeItem[]> itemsPacksSubject = new Subject<IPoeItem[]>();
        private readonly ISubject<Exception> updateExceptionsSubject = new Subject<Exception>();

        private bool isBusy;
        private TimeSpan recheckPeriod;

        public PoeLiveHistoryProvider(
            [NotNull] IPoeQueryInfo query,
            [NotNull] IPoeApiWrapper poeApi)
        {
            Guard.ArgumentNotNull(query, nameof(query));
            Guard.ArgumentNotNull(poeApi, nameof(poeApi));

            var periodObservable = this.WhenAnyValue(x => x.RecheckPeriod)
                                       .Do(LogRecheckPeriodChange)
                                       .Select(_ => ToTimer())
                                       .Switch()
                                       .Publish();

            var queryObservable = Observable.Merge(periodObservable, forceUpdatesSubject)
                                            .Where(x => !IsBusy)
                                            .Do(StartUpdate)
                                            .Select(x =>
                                            {
                                                return IsLiveMode
                                                    ? new PoeLiveUpdatesAdapter(poeApi, new PoeItemEqualityComparer()).SubscribeToLiveUpdates(query) 
                                                    : poeApi.IssueQuery(query).ToObservable().Select(y => y.ItemsList.EmptyIfNull().ToArray());
                                            })
                                            .Switch()
                                            .Do(HandleUpdate, HandleUpdateError);

            Observable
                .Defer(() => queryObservable)
                .RetryWithDelay(delayAfterError)
                .Subscribe()
                .AddTo(Anchors);

            periodObservable.Connect().AddTo(Anchors);

            Disposable.Create(() => poeApi.DisposeQuery(query)).AddTo(Anchors);
        }

        public IObservable<IPoeItem[]> ItemsPacks => itemsPacksSubject;

        public IObservable<Exception> UpdateExceptions => updateExceptionsSubject;

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

        public bool IsLiveMode => RecheckPeriod == TimeSpan.Zero;
        
        public bool IsAutoRecheckEnabled => RecheckPeriod > TimeSpan.Zero;

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
            else if (IsAutoRecheckEnabled)
            {
                return Observable.Timer(DateTimeOffset.Now, RecheckPeriod).ToUnit();
            }
            else
            {
                return Observable.Return(Unit.Default).Concat(Observable.Never<Unit>());
            }
        }

        private void StartUpdate(Unit unit)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Updating (period: {recheckPeriod})...");
            if (!IsLiveMode)
            {
                IsBusy = true;
            }
        }
        
        private void LogRecheckPeriodChange(TimeSpan newRecheckPeriod)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update period changed: {newRecheckPeriod}");
        }

        private void HandleUpdate(IPoeItem[] queryResult)
        {
            Guard.ArgumentNotNull(queryResult, nameof(queryResult));

            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update received, itemsCount: {queryResult.Length}");
            IsBusy = false;
            updateExceptionsSubject.OnNext(null);
            itemsPacksSubject.OnNext(queryResult);
        }

        private void HandleUpdateError(Exception ex)
        {
            Guard.ArgumentNotNull(ex, nameof(ex));

            Log.Instance.Error("[PoeLiveHistoryProvider] Update failed", ex);
            IsBusy = false;
            updateExceptionsSubject.OnNext(ex);
        }

        private sealed class PoeLiveUpdatesAdapter : DisposableReactiveObject
        {
            private readonly IPoeApiWrapper api;
            [NotNull] private readonly IEqualityComparer<IPoeItem> itemComparer;


            public PoeLiveUpdatesAdapter(
                [NotNull] IPoeApiWrapper api,
                [NotNull] IEqualityComparer<IPoeItem> itemComparer)
            {
                Guard.ArgumentNotNull(api, nameof(api));
                Guard.ArgumentNotNull(itemComparer, nameof(itemComparer));

                this.api = api;
                this.itemComparer = itemComparer;
            }

            public IObservable<IPoeItem[]> SubscribeToLiveUpdates([NotNull] IPoeQueryInfo query)
            {
                Guard.ArgumentNotNull(query, nameof(query));

                var items = new HashSet<IPoeItem>(itemComparer);

                return api
                       .SubscribeToLiveUpdates(query)
                       .Select(x => x.ItemsList)
                       .Select(itemsPack =>
                       {
                           foreach (var poeItem in itemsPack)
                           {
                               switch (poeItem.ItemState)
                               {
                                   case PoeTradeState.New:
                                       items.Add(poeItem);
                                       break;
                                   case PoeTradeState.Removed:
                                       items.Remove(poeItem);
                                       break;
                                   default:
                                       Log.Instance.Warn($"Invalid realtime item state {poeItem.ItemState} - {poeItem.DumpToTextRaw()}");
                                       break;
                               }
                           }
                           return items.ToArray();
                       });

            }
        }
    }
}