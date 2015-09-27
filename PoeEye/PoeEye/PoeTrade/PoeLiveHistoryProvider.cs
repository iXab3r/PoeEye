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
        private TimeSpan recheckPeriod;

        private readonly ISubject<IPoeItem[]> itemPacksSubject = new Subject<IPoeItem[]>(); 

        public PoeLiveHistoryProvider(
                [NotNull] IPoeQuery query,
                [NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => query);
            Guard.ArgumentNotNull(() => poeApi);

            this.query = query;
            this.poeApi = poeApi;

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

        public IObservable<IPoeItem[]> ItemsPacks => itemPacksSubject;

        public TimeSpan RecheckPeriod
        {
            get { return recheckPeriod; }
            set { this.RaiseAndSetIfChanged(ref recheckPeriod, value); }
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
        }

        private void HandleUpdateError(Exception ex)
        {
            Log.Instance.Error($"[PoeLiveHistoryProvider] Update failed", ex);
        }
    }
}