using System;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    public interface IPathOfExileTradeLiveApi : IDisposable
    {
        IObservable<IPoeQueryResult> Updates { get; }
    }
}