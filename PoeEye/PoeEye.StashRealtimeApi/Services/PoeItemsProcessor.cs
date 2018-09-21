using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using Newtonsoft.Json;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeItemsProcessor : DisposableReactiveObject, IPoeItemsProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeItemsProcessor));

        private readonly ConcurrentDictionary<string, IPoeItem> itemById = new ConcurrentDictionary<string, IPoeItem>();
        private readonly IEqualityComparer<IPoeItem> itemComparer;
        private readonly IPoeItemsSource itemsSource;

        private readonly ConcurrentDictionary<IPoeQueryInfo, QueryItemSource> sourcesByQuery = new ConcurrentDictionary<IPoeQueryInfo, QueryItemSource>();

        public PoeItemsProcessor(
            [NotNull] IPoeItemsSource itemsSource,
            [NotNull] IEqualityComparer<IPoeItem> itemComparer,
            [NotNull] ISchedulerProvider schedulerProvider)
        {
            Guard.ArgumentNotNull(itemsSource, nameof(itemsSource));
            Guard.ArgumentNotNull(itemComparer, nameof(itemComparer));
            Guard.ArgumentNotNull(schedulerProvider, nameof(schedulerProvider));

            this.itemsSource = itemsSource;
            this.itemComparer = itemComparer;
            itemsSource.AddTo(Anchors);

            var bgScheduler = schedulerProvider.GetOrCreate("StashRealtimeApi.ItemsProcessor");

            itemsSource.ItemPacks
                       .ObserveOn(bgScheduler)
                       .Subscribe(HandlePack, ex => Log.Error("Exception occurred", ex))
                       .AddTo(Anchors);
        }

        public IPoeQueryResult IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            var source = sourcesByQuery.GetOrAdd(query, _ => new QueryItemSource(query, itemComparer));

            var result = new PoeQueryResult
            {
                Id = Guid.NewGuid().ToString(),
                ItemsList = source.Items.ToArray()
            };

            return result;
        }

        public bool DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            QueryItemSource trash;
            return sourcesByQuery.TryRemove(query, out trash);
        }

        private void HandlePack(IPoeItem[] pack)
        {
            Log.Debug($"Got items pack, {pack.Length} element(s), total {itemById.Count}");

            foreach (var poeItem in pack)
            {
                itemById.AddOrUpdate(poeItem.Hash, poeItem, (key, oldItem) => HandleItemUpdate(oldItem, poeItem));
            }

            Log.DebugFormat("By league:\n\t{0}",
                                     pack.GroupBy(x => x.League ?? "UnknownLeague").Select(x => new {League = x.Key, Count = x.Count()}).DumpToText());
            Log.DebugFormat("By league(total):\n\t{0}",
                                     itemById.Values.GroupBy(x => x.League ?? "UnknownLeague").Select(x => new {League = x.Key, Count = x.Count()})
                                             .DumpToText());

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

        private IPoeItem HandleItemUpdate(IPoeItem oldItem, IPoeItem newItem)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(oldItem.Raw) && !string.IsNullOrWhiteSpace(newItem.Raw))
                {
                    var rawOld = JsonConvert.DeserializeObject<ExpandoObject>(oldItem.Raw);
                    var rawNew = JsonConvert.DeserializeObject<ExpandoObject>(newItem.Raw);
                    var rawComparisonResult = CompareObjects(rawOld, rawNew);
                    if (!rawComparisonResult.AreEqual)
                    {
                        Log.Debug($"Item updated RAW, key: {oldItem.Hash}\nDiff: {rawComparisonResult.DifferencesString}");
                    }
                }

                var comparisonResult = CompareObjects(oldItem, newItem);
                if (oldItem.Hash == "1c2bff3347264582830133b48b86b93e65184ddd9d347527dab41899590ff62a")
                {
                    //
                }

                if (!comparisonResult.AreEqual)
                {
                    Log.Debug($"Item updated, key: {oldItem.Hash}\nDiff: {comparisonResult.DifferencesString}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception comparing item {oldItem.Hash}", ex);
            }

            return newItem;
        }

        private static ComparisonResult CompareObjects(object thisObject, object thatObject)
        {
            var comparisonResult = new CompareLogic(
                new ComparisonConfig
                {
                    MaxDifferences = byte.MaxValue,
                    IgnoreObjectDisposedException = true,
                    ComparePrivateFields = false,
                    ComparePrivateProperties = false,
                    CompareStaticFields = false,
                    CompareStaticProperties = false,
                    SkipInvalidIndexers = true,
                    MembersToIgnore = new List<string> {nameof(IPoeItem.Timestamp), nameof(IPoeItem.Raw)}
                }).Compare(thisObject, thatObject);
            return comparisonResult;
        }

        private IPoeItem[] GetMatchingItems(IPoeQueryInfo query, IEnumerable<IPoeItem> itemsPack)
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

        private sealed class QueryItemSource : DisposableReactiveObject
        {
            private readonly ISet<IPoeItem> items;

            public QueryItemSource(IPoeQueryInfo query, IEqualityComparer<IPoeItem> comparer)
            {
                items = new HashSet<IPoeItem>(comparer);
                Query = query;
            }

            public IPoeQueryInfo Query { get; }

            public IEnumerable<IPoeItem> Items => items;

            public void AddItems(IPoeItem[] itemsPack)
            {
                Log.Debug($"Got {itemsPack.Length} items");
                var initialCount = items.Count;
                foreach (var poeItem in itemsPack)
                {
                    items.Add(poeItem);
                }

                var newItemsCount = items.Count - initialCount;
                Log.Debug($"New items count: {newItemsCount}");
            }
        }
    }
}