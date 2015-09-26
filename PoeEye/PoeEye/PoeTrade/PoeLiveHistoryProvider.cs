namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.ObjectModel;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal sealed class PoeLiveHistoryProvider : ReactiveObject, IPoeLiveHistoryProvider
    {
        private readonly IPoeQuery query;
        private readonly IPoeApi poeApi;
        private readonly IClock clock;
        private TimeSpan recheckPeriod;
        private DateTime lastUpdateTimestamp;

        private ISubject<IPoeItem[]> itemPacksSubject = new Subject<IPoeItem>(); 

        public PoeLiveHistoryProvider(
                [NotNull] IPoeQuery query,
                [NotNull] IPoeApi poeApi,
                [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(() => query);
            Guard.ArgumentNotNull(() => poeApi);
            Guard.ArgumentNotNull(() => clock);

            this.query = query;
            this.poeApi = poeApi;
            this.clock = clock;

            this.ObservableForProperty(x => x.RecheckPeriod)
                .Select(x => x.Value)
                .Do(LogRecheckPeriodChange)
                .Select(timeout => timeout == TimeSpan.Zero 
                                            ? Observable.Never<Unit>() 
                                            : Observable.Timer(DateTimeOffset.Now, timeout).Select(x => Unit.Default))
                .Switch()
                .Do(_ => LogTimestamp())
                .Select(x => poeApi.IssueQuery(query))
                .Switch()
                .Do(UpdateItems, HandleUpdateError)
                .Select(x => x.ItemsList)
                .Subscribe(itemPacksSubject);
        }

        public IObservable<IPoeItem[]> ItemPacks => itemPacksSubject;

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

        private void LogTimestamp()
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Updating (period: {recheckPeriod})...");
        }

        private void LogRecheckPeriodChange(TimeSpan newRecheckPeriod)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update period changed: {newRecheckPeriod}");
        }

        private void UpdateItems(IPoeQueryResult queryResult)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update received, itemsCount: {queryResult.ItemsList.Length}");
            LastUpdateTimestamp = clock.CurrentTime;
        }

        private void HandleUpdateError(Exception ex)
        {
            Log.Instance.Error($"[PoeLiveHistoryProvider] Update failed", ex);
        }
    }
}