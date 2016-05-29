using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nest;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.ExileToolsApi
{
    public class ExileToolsApi : IPoeApi
    {
        private readonly ElasticClient client;

        public ExileToolsApi()
        {
            const string apiKey = "b0fdb9af1a40437647ae3c45a6ebcae7";
            const string address = "http://api.exiletools.com/index";

            var settings = new ConnectionSettings(new Uri(address))
                .BasicAuthentication("apikey", apiKey)
                .DisableDirectStreaming();
            client = new ElasticClient(settings);
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQuery query)
        {
            return Observable
                .Return(IssueQueryInternal(query))
                .ToTask();
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            return Observable
                 .Return(RequestStaticDataInternal())
                 .ToTask();
        }

        private IPoeStaticData RequestStaticDataInternal()
        {
            var response = client.Search<EmptyResponse>(s => s.Aggregations(a => a.Terms("leagues", st => st.Field("attributes.league"))));
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

            var result = new PoeStaticData()
            {
                ModsList = new []
                    {
                        LoadMods(@"http://api.exiletools.com/endpoints/mapping?field=mods.*.explicit", PoeModType.Explicit, false),
                        LoadMods(@"http://api.exiletools.com/endpoints/mapping?field=mods.*.implicit", PoeModType.Implicit, false),
                    }
                    .SelectMany(x => x)
                    .Distinct(PoeItemMod.CodeNameComparer)
                    .Cast<IPoeItemMod>()
                    .ToArray(),
                LeaguesList = leaguesList,
                CurrenciesList = new IPoeCurrency[]
                {
                    new PoeCurrency {Name = "Blessed Orb", CodeName = "blessed"},
                    new PoeCurrency {Name = "Cartographer's Chisel", CodeName = "chisel"},
                    new PoeCurrency {Name = "Chaos Orb", CodeName = "chaos"},
                    new PoeCurrency {Name = "Chromatic Orb", CodeName = "chromatic"},
                    new PoeCurrency {Name = "Divine Orb", CodeName = "divine"},
                    new PoeCurrency {Name = "Exalted Orb", CodeName = "exalted"},
                    new PoeCurrency {Name = "Gemcutter's Prism", CodeName = "gcp"},
                    new PoeCurrency {Name = "Jeweller's Orb", CodeName = "jewellers"},
                    new PoeCurrency {Name = "Orb of Alchemy", CodeName = "alchemy"},
                    new PoeCurrency {Name = "Orb of Alteration", CodeName = "alteration"},
                    new PoeCurrency {Name = "Orb of Chance", CodeName = "chance"},
                    new PoeCurrency {Name = "Orb of Fusing", CodeName = "fusing"},
                    new PoeCurrency {Name = "Orb of Regret", CodeName = "regret"},
                    new PoeCurrency {Name = "Orb of Scouring", CodeName = "scouring"},
                    new PoeCurrency {Name = "Regal Orb", CodeName = "regal"}
                },
                ItemTypes = new IPoeItemType[]
                {
                    new PoeItemType {Name = "Generic One-Handed Weapon", CodeName = "1h"},
                    new PoeItemType {Name = "Generic Two-Handed Weapon", CodeName = "2h"},
                    new PoeItemType {Name = "Bow", CodeName = "Bow"},
                    new PoeItemType {Name = "Claw", CodeName = "Claw"},
                    new PoeItemType {Name = "Dagger", CodeName = "Dagger"},
                    new PoeItemType {Name = "One Hand Axe", CodeName = "One Hand Axe"},
                    new PoeItemType {Name = "One Hand Mace", CodeName = "One Hand Mace"},
                    new PoeItemType {Name = "One Hand Sword", CodeName = "One Hand Sword"},
                    new PoeItemType {Name = "Sceptre", CodeName = "Sceptre"},
                    new PoeItemType {Name = "Staff", CodeName = "Staff"},
                    new PoeItemType {Name = "Two Hand Axe", CodeName = "Two Hand Axe"},
                    new PoeItemType {Name = "Two Hand Mace", CodeName = "Two Hand Mace"},
                    new PoeItemType {Name = "Two Hand Sword", CodeName = "Two Hand Sword"},
                    new PoeItemType {Name = "Wand", CodeName = "Wand"},
                    new PoeItemType {Name = "Body Armour", CodeName = "Body Armour"},
                    new PoeItemType {Name = "Boots", CodeName = "Boots"},
                    new PoeItemType {Name = "Gloves", CodeName = "Gloves"},
                    new PoeItemType {Name = "Helmet", CodeName = "Helmet"},
                    new PoeItemType {Name = "Shield", CodeName = "Shield"},
                    new PoeItemType {Name = "Amulet", CodeName = "Amulet"},
                    new PoeItemType {Name = "Belt", CodeName = "Belt"},
                    new PoeItemType {Name = "Currency", CodeName = "Currency"},
                    new PoeItemType {Name = "Divination Card", CodeName = "Divination Card"},
                    new PoeItemType {Name = "Fishing Rods", CodeName = "Fishing Rods"},
                    new PoeItemType {Name = "Flask", CodeName = "Flask"},
                    new PoeItemType {Name = "Gem", CodeName = "Gem"},
                    new PoeItemType {Name = "Jewel", CodeName = "Jewel"},
                    new PoeItemType {Name = "Map", CodeName = "Map"},
                    new PoeItemType {Name = "Quiver", CodeName = "Quiver"},
                    new PoeItemType {Name = "Ring", CodeName = "Ring"},
                    new PoeItemType {Name = "Vaal Fragments", CodeName = "Vaal Fragments"}
                }
            };
            return result;
        }

        private IEnumerable<PoeItemMod> LoadMods(string uri, PoeModType modType, bool isCrafted)
        {
            var page = new WebClient().DownloadString(uri);

            var regex = new Regex(@"(?:implicit|explicit|crafted)\.(.+?)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (var match in page
                .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x + ".")
                .Select(x => regex.Match(x))
                .Where(x => x.Success))
            {
                yield return new PoeItemMod
                {
                    Name = match.Groups[1].Value,
                    CodeName = match.Groups[1].Value,
                    ModType = modType,
                    IsCrafted = isCrafted
                };
            }
        }

        private IPoeQueryResult IssueQueryInternal(IPoeQuery query)
        {
            var eQuery = new SearchRequest()
            {
                Query = new BoolQuery()
                {
                    Must = new QueryContainer[]
                    {
                        new TermRangeQuery()
                {
                    Field = "propertiesPseudo.Weapon.estimatedQ20.Physical DPS",
                    GreaterThan = "300",
                },

                    },
                },
                Size = 1,
                From = 0,

            };

            var health = client.Search<ExTzItem>(eQuery);
            Console.WriteLine(health.Hits.Select(x => x.Source).DumpToText());

            var converter = new ToPoeItemConverter();
            var convertedItems = health.Hits.Select(x => x.Source).Select(converter.Convert).ToArray();
            return new PoeQueryResult()
            {
                ItemsList = convertedItems,
            };
        }

        private sealed class EmptyResponse { }
    }
}
