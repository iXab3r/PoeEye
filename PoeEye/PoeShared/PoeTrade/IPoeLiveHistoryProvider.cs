namespace PoeShared.PoeTrade
{
    using System;

    using Common;

    using JetBrains.Annotations;

    public interface IPoeLiveHistoryProvider : IDisposable
    {
        IObservable<IPoeItem[]> ItemsPacks {[NotNull] get; }

        IObservable<Exception> UpdateExceptions { [NotNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        bool IsBusy { get; }
    }
}