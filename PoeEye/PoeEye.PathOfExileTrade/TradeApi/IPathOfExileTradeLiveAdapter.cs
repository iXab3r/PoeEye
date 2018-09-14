using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    public interface IPathOfExileTradeLiveAdapter : IDisposable
    {
        IObservable<IPoeQueryResult> Updates { [NotNull] get; }
    }
}