using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PoeTradeRealtimeApi
{
    public interface IRealtimeItemSource : IDisposable
    {
        bool IsDisposed { get; }

        [NotNull]
        IPoeQueryResult GetResult();
    }
}