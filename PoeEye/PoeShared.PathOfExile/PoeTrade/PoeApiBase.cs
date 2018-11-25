﻿using System;
using System.Threading.Tasks;
using Guards;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public abstract class PoeApiBase : DisposableReactiveObject, IPoeApi
    {
        public abstract Guid Id { get; }

        public abstract string Name { get; }

        public abstract bool IsAvailable { get; }

        public virtual IObservable<IPoeQueryResult> SubscribeToLiveUpdates(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            throw new NotImplementedException();
        }

        public abstract Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query);

        public abstract Task<IPoeStaticData> RequestStaticData();

        public virtual void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));
        }
    }
}