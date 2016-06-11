using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeEye.ExileToolsApi.RealtimeApi;
using PoeShared;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Regions.Behaviors;

namespace PoeEye.ExileToolsApi
{
    internal sealed class ExileToolsRealtimeApi : IPoeApi
    {
        private static readonly TimeSpan CleanupPeriod = TimeSpan.FromMinutes(1);

        private readonly ExileToolsSource exileSource;
        private readonly IFactory<IRealtimeItemSource, IPoeQueryInfo> itemSourceFactory;

        private readonly ConcurrentDictionary<IPoeQueryInfo, IRealtimeItemSource> itemSources = new ConcurrentDictionary<IPoeQueryInfo, IRealtimeItemSource>(PoeQueryInfo.Comparer);

        public ExileToolsRealtimeApi(
            [NotNull] ExileToolsSource exileSource,
            [NotNull] IFactory<IRealtimeItemSource, IPoeQueryInfo> itemSourceFactory)
        {
            Guard.ArgumentNotNull(() => exileSource);
            Guard.ArgumentNotNull(() => itemSourceFactory);

            this.exileSource = exileSource;
            this.itemSourceFactory = itemSourceFactory;

            Observable.Timer(DateTimeOffset.Now, CleanupPeriod).Subscribe(CleanupSources);
        }

        public string Name { get; } = "(alpha) ExileTools Realtime";

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Log.Instance.Debug($"[ExileToolsRealtimeApi.IssueQuery] Issueing query: {query}");

            return Observable
                .Start(() => IssueQueryInternal(query), Scheduler.Default)
                .ToTask();
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            Log.Instance.Debug($"[ExileToolsRealtimeApi.RequestStaticData] Requesting data...");
            return Observable
                .Start(exileSource.LoadStaticData, Scheduler.Default)
                .ToTask();
        }

        private IPoeQueryResult IssueQueryInternal(IPoeQueryInfo query)
        {
            IRealtimeItemSource source;
            if (!itemSources.TryGetValue(query, out source))
            {
                Log.Instance.Debug($"[ExileToolsRealtimeApi.IssueQuery] Client for query was not found, creating a new one: {query}");
                source = itemSources[query] = itemSourceFactory.Create(query);
            }

            return source.GetResult();
        }

        private void CleanupSources()
        {
            var sourcesToRemove = itemSources
                .Where(x => x.Value.IsDisposed)
                .ToArray();

            if (!sourcesToRemove.Any())
            {
                Log.Instance.Debug($"[ExileToolsRealtimeApi.CleanupSources] All sources are alive, count: {itemSources.Count}");
                return;
            }

            Log.Instance.Debug($"[ExileToolsRealtimeApi.CleanupSources] Disposed sources(count: {itemSources.Count}, disposed: {sourcesToRemove.Length}):\n {sourcesToRemove.DumpToText()}");

            foreach (var kvp in sourcesToRemove)
            {
                IRealtimeItemSource removedSource;
                itemSources.TryRemove(kvp.Key, out removedSource);
            }
        }
    }
}