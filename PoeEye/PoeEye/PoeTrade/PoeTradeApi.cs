using System;
using PoeShared;

namespace PoeEye.PoeTrade
{
    internal sealed class PoeTradeApi : IPoeApi
    {
        public IObservable<IPoeSearchResult> IssueQuery(IPoeSearchQuery query)
        {
            return null;
        }
    }
}