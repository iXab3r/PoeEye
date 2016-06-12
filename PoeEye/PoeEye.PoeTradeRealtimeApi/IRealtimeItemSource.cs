using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PoeTradeRealtimeApi
{
    public interface IRealtimeItemSource : IDisposable
    {
        [NotNull]
        IPoeQueryResult GetResult();

        bool IsDisposed { get; }
    }
}