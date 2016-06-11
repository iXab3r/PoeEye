using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.ExileToolsApi.RealtimeApi
{
    public interface IBlockItemSource : IDisposable
    {
        [NotNull]
        IPoeQueryResult GetResult();

        bool IsDisposed { get; }
    }
}