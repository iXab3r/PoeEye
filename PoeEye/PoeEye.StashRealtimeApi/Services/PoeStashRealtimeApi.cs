using System;
using System.Threading.Tasks;
using Anotar.Log4Net;
using Guards;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeStashRealtimeApi : DisposableReactiveObject, IPoeApi
    {
        [NotNull] private readonly IPoeItemsProcessor itemsSource;
        public Guid Id { get; } = Guid.Parse("A4177288-05E6-475D-B05C-A30795FF600E");

        public string Name { get; } = "Stash Realtime API";
        
        public bool IsAvailable { get; } = true;

        public PoeStashRealtimeApi([NotNull] IPoeItemsProcessor itemsSource)
        {
            Guard.ArgumentNotNull(itemsSource, nameof(itemsSource));

            this.itemsSource = itemsSource;
            itemsSource.AddTo(Anchors);
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            LogTo.Debug($"Issueing query: {query}");
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
