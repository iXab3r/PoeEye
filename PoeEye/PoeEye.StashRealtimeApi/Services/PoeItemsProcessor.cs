using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Anotar.Log4Net;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeItemsProcessor : DisposableReactiveObject, IPoeItemsProcessor
    {
        private readonly IPoeItemsSource itemsSource;
        private readonly IEqualityComparer<IPoeItem> itemComparer;

        private readonly ConcurrentDictionary<IPoeQueryInfo, QueryItemSource> sourcesByQuery = new ConcurrentDictionary<IPoeQueryInfo, QueryItemSource>();

        private readonly ConcurrentDictionary<string, IPoeItem> itemById = new ConcurrentDictionary<string, IPoeItem>();

        public PoeItemsProcessor(
            [NotNull] IPoeItemsSource itemsSource,
            [NotNull] IEqualityComparer<IPoeItem> itemComparer)
        {
            Guard.ArgumentNotNull(() => itemsSource);
            Guard.ArgumentNotNull(() => itemComparer);

            this.itemsSource = itemsSource;
            this.itemComparer = itemComparer;
            itemsSource.AddTo(Anchors);

            itemsSource.ItemPacks
                .Subscribe(HandlePack)
                .AddTo(Anchors);
        }

        public IPoeQueryResult IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(() => query);

            var source = sourcesByQuery.GetOrAdd(query, _ => new QueryItemSource(query, itemComparer));

            var result = new PoeQueryResult
            {
                Id = Guid.NewGuid().ToString(),
                ItemsList = source.Items.ToArray()
            };

            return result;
        }

        private void HandlePack(IPoeItem[] pack)
        {
            LogTo.Debug($"Got items pack, {pack.Length} element(s), total {itemById.Count}");

            foreach (var poeItem in pack)
            {
                itemById.AddOrUpdate(poeItem.Hash, poeItem, (key, oldItem) => poeItem);
            }

            LogTo.Debug("By league:\n\t{0}", pack.GroupBy(x => x.League ?? "UnknownLeague").Select(x => new { League = x.Key, Count = x.Count() }).DumpToText());
            LogTo.Debug("By league(total):\n\t{0}", itemById.Values.GroupBy(x => x.League ?? "UnknownLeague").Select(x => new { League = x.Key, Count = x.Count() }).DumpToText());


            foreach (var queryItemSource in sourcesByQuery.Values)
            {
                var matches = GetMatchingItems(queryItemSource.Query, pack);
                if (!matches.Any())
                {
                    continue;
                }
                queryItemSource.AddItems(matches);
            }
        }

        private IPoeItem[] GetMatchingItems(IPoeQueryInfo query, IEnumerable<IPoeItem> itemsPack )
        {
            return itemsPack.Where(x => IsMatch(query, x)).ToArray();
        }

        private bool IsMatch(IPoeQueryInfo query, IPoeItem item)
        {
            if (string.IsNullOrWhiteSpace(item.League) || !item.League.StartsWith("Legacy"))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(query.ItemName) && !string.IsNullOrWhiteSpace(item.ItemName) 
                && item.ItemName.IndexOf(query.ItemName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
            return true;
        }

        public bool DisposaQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(() => query);

            QueryItemSource trash;
            return sourcesByQuery.TryRemove(query, out trash);
        }

        private sealed class QueryItemSource : DisposableReactiveObject
        {
            private readonly ISet<IPoeItem> items;

            public QueryItemSource(IPoeQueryInfo query, IEqualityComparer<IPoeItem> comparer)
            {
                items = new HashSet<IPoeItem>(comparer);
                Query = query;
            }

            public IPoeQueryInfo Query { get; }

            public IEnumerable<IPoeItem> Items
            {
                get { return items; }
            }

            public void AddItems(IPoeItem[] itemsPack)
            {
                LogTo.Debug($"Got {itemsPack.Length} items");
                var initialCount = items.Count;
                foreach (var poeItem in itemsPack)
                {
                    items.Add(poeItem);
                }
                var newItemsCount = items.Count - initialCount;
                LogTo.Debug($"New items count: {newItemsCount}");
            }
        }
    }
}