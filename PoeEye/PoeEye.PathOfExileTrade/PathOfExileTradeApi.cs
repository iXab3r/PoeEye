using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeEye.PathOfExileTrade.Modularity;
using PoeEye.PathOfExileTrade.TradeApi;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using RestEase;
using TypeConverter;

namespace PoeEye.PathOfExileTrade
{
    internal sealed class PathOfExileTradeApi : DisposableReactiveObject, IPoeApi
    {
        private static readonly string TradeSearchUri = @"https://www.pathofexile.com/api/trade";
        private readonly IFactory<PoeItemBuilder> itemBuilder;
        private readonly IConverter<IPoeQueryInfo, JsonSearchRequest.Query> queryConverter;
        private readonly IClock clock;

        private readonly SemaphoreSlim requestsSemaphore;

        private PathOfExileTradeConfig config = new PathOfExileTradeConfig();

        public PathOfExileTradeApi(
            [NotNull] IFactory<PoeItemBuilder> itemBuilder,
            [NotNull] IConverter<IPoeQueryInfo, JsonSearchRequest.Query> queryConverter,
            [NotNull] IConfigProvider<PathOfExileTradeConfig> configProvider,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(queryConverter, nameof(queryConverter));
            Guard.ArgumentNotNull(itemBuilder, nameof(itemBuilder));
            Guard.ArgumentNotNull(clock, nameof(clock));
            
            this.itemBuilder = itemBuilder;
            this.queryConverter = queryConverter;
            this.clock = clock;

            configProvider
                .WhenChanged
                .Subscribe(x => config = x)
                .AddTo(Anchors);
            Log.Instance.Debug($"[PathOfExileTradeApi..ctor] {config.DumpToText()}");
            requestsSemaphore = new SemaphoreSlim(config.MaxSimultaneousRequestsCount);
        }

        public Guid Id { get; } = Guid.Parse("8E846305-0D14-4C55-9E47-DF31423833BA");

        public string Name { get; } = "pathofexile.com/trade";

        public bool IsAvailable { get; } = true;

        public async Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(queryInfo, nameof(queryInfo));

            var client = CreateClient();

            try
            {
                Log.Instance.Debug(
                    $"[PathOfExileTradeApi] Awaiting for semaphore slot (max: {config.MaxSimultaneousRequestsCount}, atm: {requestsSemaphore.CurrentCount})");
                await requestsSemaphore.WaitAsync();

                var query = new JsonSearchRequest.Request
                {
                    Query = queryConverter.Convert(queryInfo),
                    Sort = new JsonSearchRequest.Sort
                    {
                        Price = "asc"
                    }
                };

                var resultIds = (await client.Search(queryInfo.League, query)).GetContentEx();

                Log.Instance.Trace($"[PathOfExileTradeApi.Api] Got {resultIds.Total} entries as a result");

                const int maxItemsInRequest = 10;
                const int maxItemsToProcess = 30;
                var listings = new ConcurrentBag<JsonFetchRequest.ResultListing>();
                if (resultIds.Result.Length > 0)
                {
                    foreach (var partition in Partitioner
                                              .Create(0, Math.Min(maxItemsToProcess, resultIds.Result.Length), maxItemsInRequest)
                                              .GetDynamicPartitions())
                    {
                        var itemIds = new ArraySegment<string>(resultIds.Result, partition.Item1, partition.Item2 - partition.Item1);
                        var batchResult = (await client.FetchItems(string.Join(",", itemIds), resultIds.Id)).GetContentEx();
                        batchResult.Listings.ForEach(listings.Add);
                    }
                }

                return new PoeQueryResult
                {
                    Id = resultIds.Id,
                    ItemsList = listings.EmptyIfNull().Where(x => !x.Gone ?? true).Select(x =>
                    {
                        var result = itemBuilder.Create()
                                                .WithStashItem(x.Item)
                                                .WithIndexationTimestamp(x.Listing?.Indexed)
                                                .WithTimestamp(clock.Now)
                                                .WithPrivateMessage(x.Listing?.Whisper)
                                                .WithRawPrice(x.Listing?.Price?.ToString())
                                                .WithUserIgn(x.Listing?.Account?.LastCharacterName)
                                                .WithUserForumName(x.Listing?.Account?.Name)
                                                .WithOnline(x.Listing?.Account?.Online != null)
                                                .Build();
                        return result;
                    }).ToArray()
                };
            }
            finally
            {
                ReleaseSemaphore();
            }
        }

        public async Task<IPoeStaticData> RequestStaticData()
        {
            var client = CreateClient();

            var result = new PoeStaticData();
            var apiLeagueList = await client.GetLeagueList();
            result.LeaguesList = apiLeagueList.GetContentEx().Result.EmptyIfNull().Select(x => x.Id).ToArray();

            var statsList = (await client.GetStatsList()).GetContentEx();

            result.ModsList = statsList.Categories
                                       .SelectMany(x => x.Entries)
                                       .Select(x => new PoeItemMod
                                       {
                                           Name = $"({x.StatsType}) {x.Text}",
                                           ModType = x.StatsType.ToPoeModType(),
                                           Origin = x.StatsType.ToPoeModOrigin(),
                                           CodeName = x.Id
                                       })
                                       .OfType<IPoeItemMod>()
                                       .ToArray();

            var staticData = (await client.GetStatic()).GetContentEx();
            result.CurrenciesList = staticData.Result.Currency.EmptyIfNull().Select(x => new PoeCurrency
            {
                Name = x.Text,
                CodeName = x.Id,
                IconUri = x.Image
            }).ToArray();

            result.ItemTypes = new IPoeItemType[]
            {
                new PoeItemType("Any Weapon", "weapon"),
                new PoeItemType("One-Handed Melee Weapon", "weapon.onemelee"),
                new PoeItemType("Two-Handed Melee Weapon", "weapon.twomelee"),
                new PoeItemType("Bow", "weapon.bow"),
                new PoeItemType("Claw", "weapon.claw"),
                new PoeItemType("Dagger", "weapon.dagger"),
                new PoeItemType("One-Handed Axe", "weapon.oneaxe"),
                new PoeItemType("One-Handed Mace", "weapon.onemace"),
                new PoeItemType("One-Handed Sword", "weapon.onesword"),
                new PoeItemType("Sceptre", "weapon.sceptre"),
                new PoeItemType("Staff", "weapon.staff"),
                new PoeItemType("Two-Handed Axe", "weapon.twoaxe"),
                new PoeItemType("Two-Handed Mace", "weapon.twomace"),
                new PoeItemType("Two-Handed Sword", "weapon.twosword"),
                new PoeItemType("Wand", "weapon.wand"),
                new PoeItemType("Fishing Rod", "weapon.rod"),
                new PoeItemType("Any Armour", "armour"),
                new PoeItemType("Body Armour", "armour.chest"),
                new PoeItemType("Boots", "armour.boots"),
                new PoeItemType("Gloves", "armour.gloves"),
                new PoeItemType("Helmet", "armour.helmet"),
                new PoeItemType("Shield", "armour.shield"),
                new PoeItemType("Quiver", "armour.quiver"),
                new PoeItemType("Any Accessory", "accessory"),
                new PoeItemType("Amulet", "accessory.amulet"),
                new PoeItemType("Belt", "accessory.belt"),
                new PoeItemType("Ring", "accessory.ring"),
                new PoeItemType("Any Gem", "gem"),
                new PoeItemType("Skill Gem", "gem.activegem"),
                new PoeItemType("Support Gem", "gem.supportgem"),
                new PoeItemType("Any Jewel", "jewel"),
                new PoeItemType("Abyss Jewel", "jewel.abyss"),
                new PoeItemType("Flask", "flask"),
                new PoeItemType("Map", "map"),
                new PoeItemType("Leaguestone", "leaguestone"),
                new PoeItemType("Prophecy", "prophecy"),
                new PoeItemType("Card", "card"),
                new PoeItemType("Captured Beast", "monster"),
                new PoeItemType("Any Currency", "currency"),
                new PoeItemType("Unique Fragment", "currency.piece"),
                new PoeItemType("Resonator", "currency.resonator"),
                new PoeItemType("Fossil", "currency.fossil")
            };

            return result;
        }

        public void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));
        }

        private IPathOfExileTradePortalApi CreateClient()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var client = new RestClient(TradeSearchUri, HandleRequestMessage)
            {
                JsonSerializerSettings = settings
            }.For<IPathOfExileTradePortalApi>();
            return client;
        }

        private async Task HandleRequestMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request != null && request.Content != null ? await request?.Content.ReadAsStringAsync() : "undefined";
            Log.Instance.Debug($"[PathOfExileTradeApi.Api] Requesting {request?.RequestUri}', content: {request?.Content.DumpToTextRaw()}, body:\n{body}");

            await Task.Delay(0, cancellationToken);
        }

        private void ReleaseSemaphore()
        {
            Log.Instance.Debug($"[PoeTradeApi] Awaiting {config.DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
            Thread.Sleep(config.DelayBetweenRequests);
            requestsSemaphore.Release();
        }

        private string ThrowIfNotParseable(string queryResult)
        {
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                throw new ApplicationException("Malformed query result - empty string");
            }

            return queryResult;
        }
    }
}