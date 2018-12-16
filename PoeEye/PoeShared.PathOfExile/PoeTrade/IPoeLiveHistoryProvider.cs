using System;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;

namespace PoeShared.PoeTrade
{
    public interface IPoeLiveHistoryProvider : IDisposable
    {
        IPoeItem[] ItemPack { [CanBeNull] get; }

        Exception LastException { [CanBeNull] get; }
        
        IPoeQueryResult QueryResult { [CanBeNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        bool IsBusy { get; }
        
        DateTime LastUpdateTimestamp { get; }
        
        void Refresh();
    }
}