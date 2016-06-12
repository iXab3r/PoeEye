using System;
using System.Threading.Tasks;
using Guards;
using PoeShared.PoeTrade.Query;

namespace PoeShared.PoeTrade
{
    public abstract class PoeApi : IPoeApi
    {
        public abstract Guid Id { get; }

        public abstract string Name { get; }

        public abstract Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query);

        public abstract Task<IPoeStaticData> RequestStaticData();

        public virtual void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(() => query);
        }
    }
}