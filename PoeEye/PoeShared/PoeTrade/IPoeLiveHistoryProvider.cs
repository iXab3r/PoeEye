using System;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade
{
    public interface IPoeLiveHistoryProvider : IDisposable
    {
        IObservable<IPoeItem[]> ItemsPacks { [NotNull] get; }

        IObservable<Exception> UpdateExceptions { [NotNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        bool IsBusy { get; }

        void Refresh();
    }
}