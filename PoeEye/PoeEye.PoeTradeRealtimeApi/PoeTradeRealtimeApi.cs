using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade;
using PoeShared;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTradeRealtimeApi
{
    internal sealed class PoeTradeRealtimeApi : DisposableReactiveObject, IPoeApi
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IFactory<IRealtimeItemSource, IPoeQueryInfo> itemSourceFactory;

        private readonly ConcurrentDictionary<IPoeQueryInfo, IRealtimeItemSource> itemSources =
            new ConcurrentDictionary<IPoeQueryInfo, IRealtimeItemSource>(PoeQueryInfo.Comparer);

        private readonly PoeTradeApi poeTradeApi;

        public PoeTradeRealtimeApi(
            [NotNull] PoeTradeApi poeTradeApi,
            [NotNull] IFactory<IRealtimeItemSource, IPoeQueryInfo> itemSourceFactory)
        {
            Guard.ArgumentNotNull(poeTradeApi, nameof(poeTradeApi));
            Guard.ArgumentNotNull(itemSourceFactory, nameof(itemSourceFactory));

            this.poeTradeApi = poeTradeApi;
            this.itemSourceFactory = itemSourceFactory;
        }

        public Guid Id { get; } = Guid.Parse("16E6A0E6-E5A4-4260-A698-764DD8B2E843");

        public string Name { get; } = "poe.trade Realtime";

        public bool IsAvailable { get; } = true;

        public IObservable<IPoeQueryResult> SubscribeToLiveUpdates(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            return Observable.Never<IPoeQueryResult>();
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Log.Instance.Debug($"[PoeTradeRealtimeApi.IssueQuery] Issueing query: {query}");

            return Observable
                   .Start(() => IssueQueryInternal(query), Scheduler.Default)
                   .ToTask();
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            Log.Instance.Debug($"[PoeTradeRealtimeApi.RequestStaticData] Requesting data...");
            return poeTradeApi.RequestStaticData();
        }

        public void DisposeQuery(IPoeQueryInfo query)
        {
            CleanupSources(query);
        }

        private IPoeQueryResult IssueQueryInternal(IPoeQueryInfo query)
        {
            IRealtimeItemSource source;
            if (!itemSources.TryGetValue(query, out source))
            {
                Log.Instance.Debug($"[PoeTradeRealtimeApi.IssueQuery] Client for query was not found, creating a new one: {query}");
                source = itemSources[query] = itemSourceFactory.Create(query);
            }

            return source.GetResult();
        }

        private void CleanupSources(params IPoeQueryInfo[] queriesToRemove)
        {
            Log.Instance.Debug(
                $"[PoeTradeRealtimeApi.CleanupSources] Disposing sources(total sources count: {itemSources.Count}, toDispose: {queriesToRemove.Length})");

            foreach (var query in queriesToRemove)
            {
                IRealtimeItemSource removedSource;
                if (itemSources.TryRemove(query, out removedSource))
                {
                    removedSource.Dispose();
                }
            }
        }
    }
}