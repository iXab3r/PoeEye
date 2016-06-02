using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Nest;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.ExileToolsApi
{
    public class ExileToolsApi : IPoeApi
    {
        private readonly IConverter<IPoeQueryInfo, ISearchRequest> queryConverter;
        private readonly ElasticClient client;

        public ExileToolsApi([NotNull] IConverter<IPoeQueryInfo, ISearchRequest> queryConverter)
        {
            Guard.ArgumentNotNull(() => queryConverter);

            const string address = "http://api.exiletools.com/index";
            const string apiKey = "b0fdb9af1a40437647ae3c45a6ebcae7";

            this.queryConverter = queryConverter;
            var settings = new ConnectionSettings(new Uri(address))
                .BasicAuthentication("apikey", apiKey)
                .DisableDirectStreaming();
            client = new ElasticClient(settings);
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            return Observable
                .Start(() => IssueQueryInternal(query), Scheduler.Default)
                .ToTask();
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            return Observable
                 .Start(RequestStaticDataInternal, Scheduler.Default)
                 .ToTask();
        }

        private IPoeStaticData RequestStaticDataInternal()
        {
            var result = new PoeStaticData()
            {
                ModsList = LoadMods(),
                LeaguesList = LoadLeagues(),
                CurrenciesList = LoadCurrencies(),
                ItemTypes = LoadItemTypes(),
            };
            return result;
        }

        private string[] LoadLeagues()
        {
            var response = client.Search<EmptyResponse>(s => s.Aggregations(a => a.Terms("leagues", st => st.Field("attributes.league"))).Size(0));
            var leaguesList = response
                .Aggregations
                .Select(x => x.Value as BucketAggregate)
                .Where(x => x?.Items != null)
                .SelectMany(x => x.Items)
                .Select(x => x as KeyedBucket)
                .Where(x => x != null)
                .Select(x => x.Key)
                .Distinct()
                .ToArray();

            return leaguesList;
        }

        private IPoeCurrency[] LoadCurrencies()
        {
            return new IPoeCurrency[]
            {
                new PoeCurrency {Name = "Blessed Orb", CodeName = KnownCurrencyNameList.BlessedOrb},
                new PoeCurrency {Name = "Cartographer's Chisel", CodeName = KnownCurrencyNameList.CartographersChisel},
                new PoeCurrency {Name = "Chaos Orb", CodeName = KnownCurrencyNameList.ChaosOrb},
                new PoeCurrency {Name = "Chromatic Orb", CodeName = KnownCurrencyNameList.ChromaticOrb},
                new PoeCurrency {Name = "Divine Orb", CodeName = KnownCurrencyNameList.DivineOrb},
                new PoeCurrency {Name = "Exalted Orb", CodeName = KnownCurrencyNameList.ExaltedOrb},
                new PoeCurrency {Name = "Gemcutter's Prism", CodeName = KnownCurrencyNameList.GemcuttersPrism},
                new PoeCurrency {Name = "Jeweller's Orb", CodeName = KnownCurrencyNameList.JewellersOrb},
                new PoeCurrency {Name = "Orb of Alchemy", CodeName = KnownCurrencyNameList.OrbOfAlchemy},
                new PoeCurrency {Name = "Orb of Alteration", CodeName = KnownCurrencyNameList.OrbOfAlteration},
                new PoeCurrency {Name = "Orb of Chance", CodeName = KnownCurrencyNameList.OrbOfChance},
                new PoeCurrency {Name = "Orb of Fusing", CodeName = KnownCurrencyNameList.OrbOfFusing},
                new PoeCurrency {Name = "Orb of Regret", CodeName = KnownCurrencyNameList.OrbOfRegret},
                new PoeCurrency {Name = "Orb of Scouring", CodeName = KnownCurrencyNameList.OrbOfScouring},
                new PoeCurrency {Name = "Regal Orb", CodeName = KnownCurrencyNameList.RegalOrb},
                new PoeCurrency {Name = "Vaal Orb", CodeName = KnownCurrencyNameList.VaalOrb}
            };
        }

        private IPoeItemType[] LoadItemTypes()
        {
            var response = client
                .Search<EmptyResponse>(s =>
                    s.Aggregations(b => b.Terms("equipType", stb => stb.Field("attributes.equipType").Size(byte.MaxValue)
                     .Aggregations(c => c.Terms("itemType", stc => stc.Field("attributes.itemType"))).Size(byte.MaxValue)))
                     .Size(0));

            var eqBuckets =
                from row in response.Aggregations
                where row.Value is BucketAggregate
                from eqBucket in ((BucketAggregate)row.Value).Items.OfType<KeyedBucket>()
                from eqRow in eqBucket.Aggregations
                where eqRow.Value is BucketAggregate
                from itBucket in ((BucketAggregate)eqRow.Value).Items.OfType<KeyedBucket>()
                select new { EquipType = eqBucket.Key, ItemType = itBucket.Key };


            var result = new List<IPoeItemType>
            {
                new PoeItemType() {Name = "Generic One-Handed Weapon", EquipType = "One Handed Melee Weapon"},
                new PoeItemType() {Name = "Generic Two-Handed Weapon", EquipType = "Two Handed Melee Weapon"}
            };

            result.AddRange(eqBuckets.Select(x => ToPoeItemType(x.EquipType, x.ItemType)).OrderBy(x => x.EquipType));

            return result.ToArray();
        }

        private IPoeItemType ToPoeItemType(string equipType, string itemType)
        {
            if (string.IsNullOrWhiteSpace(equipType) && string.IsNullOrWhiteSpace(itemType))
            {
                return new PoeItemType { Name = "Empty" };
            }
            if (equipType == itemType)
            {
                return new PoeItemType
                {
                    EquipType = equipType,
                    ItemType = itemType,
                    Name = itemType
                };
            }

            if (equipType == "One Handed Melee Weapon")
            {
                equipType = "One Hand";
            }
            else if (equipType == "Two Handed Melee Weapon")
            {
                equipType = "Two Hand";
            }
            else if (equipType == "One Handed Projectile Weapon")
            {
                equipType = "One Hand";
            }

            return new PoeItemType
            {
                EquipType = equipType,
                ItemType = itemType,
                Name = string.Join(" - ", equipType, itemType),
            };
        }

        private IPoeItemMod[] LoadMods()
        {
            var page = new WebClient().DownloadString(@"http://api.exiletools.com/endpoints/mapping?field=mods*");

            var result = new List<PoeItemMod>();
            result.AddRange(LoadMods(page, "explicit", PoeModType.Explicit, false));
            result.AddRange(LoadMods(page, "implicit", PoeModType.Implicit, false));
            result.AddRange(LoadMods(page, "crafted", PoeModType.Unknown, true));
            result.AddRange(LoadMods(page, "modsTotal", PoeModType.Unknown, false));
            result.AddRange(LoadMods(page, "modsPseudo", PoeModType.Unknown, false));
            return result
                    .Distinct(PoeItemMod.NameComparer)
                    .Cast<IPoeItemMod>()
                    .ToArray();
        }

        private IEnumerable<PoeItemMod> LoadMods(string data, string expectedPrefix, PoeModType modType, bool isCrafted)
        {
            var regex = new Regex($@"^.*(?'prefix'{expectedPrefix})\.(?'name'[^.\n\r]+)(?'kind'\.avg)?.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (var match in data
                .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => regex.Match(x))
                .Where(x => x.Success))
            {
                var modCodeName = match.Value;

                yield return new PoeItemMod
                {
                    Name = modCodeName,
                    CodeName = modCodeName,
                    ModType = modType,
                    IsCrafted = isCrafted
                };
            }
        }

        private IPoeQueryResult IssueQueryInternal(IPoeQueryInfo queryInfo)
        {
            var query = queryConverter.Convert(queryInfo);

            var queryResult = client.Search<ExTzItem>(query);

            Log.Instance.Debug($"[ExileToolsApi] Response data:\n{queryResult.DebugInformation}");

            var converter = new ToPoeItemConverter();
            var convertedItems = queryResult.Hits.Select(x => x.Source).Select(converter.Convert).ToArray();
            return new PoeQueryResult()
            {
                ItemsList = convertedItems,
            };
        }

        private sealed class EmptyResponse { }
    }
}
