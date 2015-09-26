namespace PoeShared.PoeTrade
{
    using System;

    using Common;

    using JetBrains.Annotations;

    public interface IPoeLiveHistoryProvider
    {
        IObservable<IPoeItem> Items {[NotNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        DateTime LastUpdateTimestamp { get; }
    }
}