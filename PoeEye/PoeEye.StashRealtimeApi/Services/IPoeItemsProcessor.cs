using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;

namespace PoeEye.StashRealtimeApi.Services
{
    internal interface IPoeItemsProcessor : IDisposable
    {
        [NotNull]
        IPoeQueryResult IssueQuery([NotNull] IPoeQueryInfo query);

        bool DisposeQuery([NotNull] IPoeQueryInfo query);
    }
}