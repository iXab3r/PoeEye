namespace PoeShared.PoeTrade
{
    using System;

    using Common;

    using JetBrains.Annotations;

    public interface IPoeLiveHistoryProvider
    {
        IObservable<IPoeItem[]> ItemsPacks {[NotNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        IObservable<Exception> UpdateExceptions { [NotNull] get; }

        bool IsBusy { get; }
    }
}