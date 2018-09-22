using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared.Common;
using RestEase;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    internal interface IPathOfExileTradePortalApi
    {
        [Get("data/leagues")]
        [AllowAnyStatusCode]
        Task<Response<JsonGetLeagueListResponse>> GetLeagueList();

        [Get("data/stats")]
        [AllowAnyStatusCode]
        Task<Response<JsonGetStatsListResponse>> GetStatsList();

        [Get("data/static")]
        [AllowAnyStatusCode]
        Task<Response<JsonGetStaticResponse>> GetStatic();

        [Post("search/{league}")]
        [AllowAnyStatusCode]
        Task<Response<JsonSearchRequest.Response>> Search([Path] string league, [Body] JsonSearchRequest.Request query);

        [Get("fetch/{csvItemIdList}")]
        [AllowAnyStatusCode]
        Task<Response<JsonFetchRequest.Response>> FetchItems([Path] string csvItemIdList, string queryId);
    }

    internal struct JsonGetLeagueListResponse
    {
        [JsonProperty("result")]
        public List<JsonLeague> Result { get; set; }
    }

    internal class JsonLeague
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    internal class JsonGetStatsListResponse
    {
        [JsonProperty("result")]
        public JsonStatsCategory[] Categories { get; set; }
    }

    internal class JsonStatsCategory
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("entries")]
        public JsonStatsEntry[] Entries { get; set; }
    }

    internal class JsonStatsEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public StatsTypeEnum StatsType { get; set; }
    }

    internal enum StatsTypeEnum
    {
        Crafted,
        Delve,
        Enchant,
        Explicit,
        Implicit,
        Monster,
        Pseudo
    }

    public class JsonGetStaticResponse
    {
        [JsonProperty("result")]
        public JsonStaticEntries Result { get; set; }
    }

    public class JsonStaticEntries
    {
        [JsonProperty("currency")]
        public JsonCurrency[] Currency { get; set; }

        [JsonProperty("fragments")]
        public JsonCurrency[] Fragments { get; set; }

        [JsonProperty("resonators")]
        public JsonCurrency[] Resonators { get; set; }

        [JsonProperty("fossils")]
        public JsonCurrency[] Fossils { get; set; }

        [JsonProperty("vials")]
        public JsonStaticEntry[] Vials { get; set; }

        [JsonProperty("nets")]
        public JsonStaticEntry[] Nets { get; set; }

        [JsonProperty("leaguestones")]
        public JsonStaticEntry[] Leaguestones { get; set; }

        [JsonProperty("essences")]
        public JsonCurrency[] Essences { get; set; }

        [JsonProperty("cards")]
        public JsonStaticEntry[] Cards { get; set; }

        [JsonProperty("maps")]
        public JsonStaticEntry[] Maps { get; set; }

        [JsonProperty("shaped_maps")]
        public JsonStaticEntry[] ShapedMaps { get; set; }

        [JsonProperty("elder_maps")]
        public JsonStaticEntry[] ElderMaps { get; set; }

        [JsonProperty("misc")]
        public object[] Misc { get; set; }
    }

    public class JsonStaticEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class JsonCurrency
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    internal static class StatsTypeEnumExtensions
    {
        public static PoeModType ToPoeModType(this StatsTypeEnum source)
        {
            switch (source)
            {
                case StatsTypeEnum.Explicit:
                    return PoeModType.Explicit;
                case StatsTypeEnum.Implicit:
                    return PoeModType.Implicit;
                default:
                    return PoeModType.Unknown;
            }
        }

        public static PoeModOrigin ToPoeModOrigin(this StatsTypeEnum source)
        {
            switch (source)
            {
                case StatsTypeEnum.Crafted:
                    return PoeModOrigin.Craft;
                case StatsTypeEnum.Enchant:
                    return PoeModOrigin.Craft;
                default:
                    return PoeModOrigin.Unknown;
            }
        }
    }
}