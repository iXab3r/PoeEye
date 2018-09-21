using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeStashRealtimeApi : DisposableReactiveObject, IPoeApi
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeStashRealtimeApi));

        private readonly IPoeItemsProcessor itemsSource;

        public PoeStashRealtimeApi([NotNull] IPoeItemsProcessor itemsSource)
        {
            Guard.ArgumentNotNull(itemsSource, nameof(itemsSource));

            this.itemsSource = itemsSource;
            itemsSource.AddTo(Anchors);
        }

        public Guid Id { get; } = Guid.Parse("A4177288-05E6-475D-B05C-A30795FF600E");

        public string Name { get; } = "Stash Realtime API";

        public bool IsAvailable { get; } = true;

        public IObservable<IPoeQueryResult> SubscribeToLiveUpdates(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            return Observable.Never<IPoeQueryResult>();
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            Log.Debug($"Issueing query: {query}");
            return Task.Run(() => itemsSource.IssueQuery(query));
        }

        public async Task<IPoeStaticData> RequestStaticData()
        {
            return await Task.Run(() => new PoeStaticData());
        }

        public void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            itemsSource.DisposeQuery(query);
        }
    }
}