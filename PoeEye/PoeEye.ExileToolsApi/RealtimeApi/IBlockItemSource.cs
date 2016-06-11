using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.ExileToolsApi.RealtimeApi
{
    public interface IBlockItemSource : IDisposable
    {
        void Connect();

        [NotNull]
        IPoeQueryResult GetResult();

        bool IsDisposed { get; }
    }
}