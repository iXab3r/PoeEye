﻿namespace PoeEye.PoeTrade
{
    using System;
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
        private readonly ISubject<IPoeItem[]> itemPacksSubject = new Subject<IPoeItem[]>();
        private readonly ISubject<Exception> updateExceptionsSubject = new Subject<Exception>();

        private bool isBusy;
        private TimeSpan recheckPeriod;

        public PoeLiveHistoryProvider(
            [NotNull] IPoeQuery query,
            [NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => query);
            Guard.ArgumentNotNull(() => poeApi);

            this.ObservableForProperty(x => x.RecheckPeriod)
                .Select(x => x.Value)
                .Do(LogRecheckPeriodChange)
                .Select(timeout => timeout == TimeSpan.Zero
                    ? Observable.Never<Unit>()
                    : Observable.Timer(DateTimeOffset.Now, timeout).Select(x => Unit.Default))
                .Switch()
                .Do(StartUpdate)
                .Select(x => poeApi.IssueQuery(query))
                .Switch()
                .Do(HandleUpdate, HandleUpdateError)
                .Select(x => x.ItemsList)
                .Subscribe(itemPacksSubject);
        }

        public IObservable<IPoeItem[]> ItemsPacks => itemPacksSubject;

        public IObservable<Exception> UpdateExceptions => updateExceptionsSubject;

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public TimeSpan RecheckPeriod
        {
            get { return recheckPeriod; }
            set { this.RaiseAndSetIfChanged(ref recheckPeriod, value); }
        }

        private void StartUpdate(Unit unit)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Updating (period: {recheckPeriod})...");
            IsBusy = true;
        }

        private void LogRecheckPeriodChange(TimeSpan newRecheckPeriod)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update period changed: {newRecheckPeriod}");
        }

        private void HandleUpdate(IPoeQueryResult queryResult)
        {
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update received, itemsCount: {queryResult.ItemsList.Length}");
            IsBusy = false;
        }

        private void HandleUpdateError(Exception ex)
        {
            Log.Instance.Error($"[PoeLiveHistoryProvider] Update failed", ex);
            updateExceptionsSubject.OnNext(ex);
        }
    }
}