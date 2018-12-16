using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PoeTrade.TradeApi
{
    public interface IPoeTradeLiveAdapter : IDisposable
    {
        IObservable<IPoeQueryResult> Updates { [NotNull] get; }
    }
}