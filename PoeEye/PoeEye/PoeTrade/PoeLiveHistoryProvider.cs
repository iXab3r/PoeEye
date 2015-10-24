namespace PoeEye.PoeTrade
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
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class PoeLiveHistoryProvider : DisposableReactiveObject, IPoeLiveHistoryProvider
    {
        private readonly TimeSpan recheckPeriodThrottling = TimeSpan.FromMilliseconds(1000);

        private readonly ISubject<IPoeItem[]> itemsPacksSubject = new Subject<IPoeItem[]>();
        private readonly ISubject<Exception> updateExceptionsSubject = new Subject<Exception>();

        private bool isBusy;
        private TimeSpan recheckPeriod;

        public PoeLiveHistoryProvider(
            [NotNull] IPoeQuery query,
            [NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => query);
            Guard.ArgumentNotNull(() => poeApi);
            
            var periodObservable = this.ObservableForProperty(x => x.RecheckPeriod)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .Throttle(recheckPeriodThrottling)
                .Do(LogRecheckPeriodChange)
                .Select(timeout => timeout == TimeSpan.Zero
                    ? Observable.Never<Unit>()
                    : Observable.Timer(DateTimeOffset.Now, timeout).ToUnit())
                .Switch()
                .Publish();

            var queryObservable = periodObservable
                .Where(x => !IsBusy)
                .Do(StartUpdate)
                .Select(x => poeApi.IssueQuery(query))
                .Switch()
                .Select(x => x.ItemsList)
                .Do(HandleUpdate, HandleUpdateError);

            Observable
                .Defer(() => queryObservable)
                .Retry()
                .Subscribe()
                .AddTo(Anchors);

            periodObservable.Connect().AddTo(Anchors);
        }

        public IObservable<IPoeItem[]> ItemsPacks => itemsPacksSubject;

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

        private void HandleUpdate(IPoeItem[] queryResult)
        {
            Guard.ArgumentNotNull(() => queryResult);
            
            Log.Instance.Debug($"[PoeLiveHistoryProvider] Update received, itemsCount: {queryResult.Length}");
            IsBusy = false;
            updateExceptionsSubject.OnNext(null);
            itemsPacksSubject.OnNext(queryResult);
        }

        private void HandleUpdateError(Exception ex)
        {
            Guard.ArgumentNotNull(() => ex);
            
            Log.Instance.Error($"[PoeLiveHistoryProvider] Update failed", ex);
            IsBusy = false;
            updateExceptionsSubject.OnNext(ex);
        }
    }
}