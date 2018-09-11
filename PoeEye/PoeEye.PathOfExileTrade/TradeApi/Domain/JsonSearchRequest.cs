using Newtonsoft.Json;

namespace PoeEye.PathOfExileTrade.TradeApi.Domain
{
    public static class JsonSearchRequest
    {
        public partial class Request
        {
            [JsonProperty("query")]
            public Query Query { get; set; }

            [JsonProperty("sort")]
            public Sort Sort { get; set; }
        }
        
        public partial class Response
        {
            [JsonProperty("result")]
            public string[] Result { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("total")]
            public long Total { get; set; }
        }
        
        public partial class Query
        {
            [JsonProperty("term")]
            public string Term { get; set; }
            
            [JsonProperty("name")]
            public string Name { get; set; }
            
            [JsonProperty("type")]
            public string ItemBaseType { get; set; }
            
            [JsonProperty("status")]
            public OptionValue Status { get; set; }

            [JsonProperty("stats")]
            public FilterGroup[] Groups { get; set; }

            [JsonProperty("filters")]
            public QueryFilters Filters { get; set; }
        }

        public partial class QueryFilters
        {
            [JsonProperty("socket_filters")]
            public SocketFilters SocketFilters { get; set; }

            [JsonProperty("misc_filters")]
            public MiscFilters MiscFilters { get; set; }

            [JsonProperty("trade_filters")]
            public TradeFilters TradeFilters { get; set; }

            [JsonProperty("map_filters")]
            public MapFilters MapFilters { get; set; }

            [JsonProperty("req_filters")]
            public ReqFilters ReqFilters { get; set; }

            [JsonProperty("armour_filters")]
            public ArmourFilters ArmourFilters { get; set; }

            [JsonProperty("weapon_filters")]
            public WeaponFilters WeaponFilters { get; set; }

            [JsonProperty("type_filters")]
            public TypeFilters TypeFilters { get; set; }
        }

        public partial class ArmourFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public ArmourFilterDef Value { get; set; }
        }

        public partial class ArmourFilterDef : BaseFilterDef
        {
            [JsonProperty("ar")]
            public MinMaxValue Armour { get; set; }

            [JsonProperty("ev")]
            public MinMaxValue Evasion { get; set; }

            [JsonProperty("block")]
            public MinMaxValue Block { get; set; }

            [JsonProperty("es")]
            public MinMaxValue EnergyShield { get; set; }
        }

        public partial class MinMaxValue : BaseFilterDef
        {
            [JsonProperty("min")]
            public long? Min { get; set; }

            [JsonProperty("max")]
            public long? Max { get; set; }
        }

        public partial class MapFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public MapFilterDef Value { get; set; }
        }

        public partial class MapFilterDef : BaseFilterDef
        {
            [JsonProperty("disabled")]
            public bool Disabled { get; set; }
            
            [JsonProperty("map_tier")]
            public MapTier MapTier { get; set; }
        }

        public partial class MapTier : BaseFilterDef
        {
            [JsonProperty("min")]
            public long? Min { get; set; }
        }

        public partial class MiscFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public MiscFilterDef Value { get; set; }
        }

        public partial class MiscFilterDef : BaseFilterDef
        {
            [JsonProperty("quality")]
            public MinMaxValue Quality { get; set; }

            [JsonProperty("gem_level")]
            public MinMaxValue GemLevel { get; set; }

            [JsonProperty("ilvl")]
            public MinMaxValue Ilvl { get; set; }

            [JsonProperty("shaper_item")]
            public OptionValue ShaperItem { get; set; }

            [JsonProperty("alternate_art")]
            public OptionValue AlternateArt { get; set; }

            [JsonProperty("corrupted")]
            public OptionValue Corrupted { get; set; }

            [JsonProperty("enchanted")]
            public OptionValue Enchanted { get; set; }

            [JsonProperty("elder_item")]
            public OptionValue ElderItem { get; set; }

            [JsonProperty("identified")]
            public OptionValue Identified { get; set; }

            [JsonProperty("crafted")]
            public OptionValue Crafted { get; set; }

            [JsonProperty("mirrored")]
            public OptionValue Mirrored { get; set; }
        }

        public class BaseFilter : BaseFilterDef
        {
            [JsonProperty("disabled")]
            public bool Disabled { get; set; }
        }
        
        public class BaseFilterDef
        {
        }

        public partial class OptionValue
        {
            [JsonProperty("option")]
            public string Option { get; set; }
        }

        public partial class ReqFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public RequirementsFilterDef Value { get; set; }
        }

        public partial class RequirementsFilterDef : BaseFilterDef
        {
            [JsonProperty("lvl")]
            public MinMaxValue Lvl { get; set; }

            [JsonProperty("str")]
            public MinMaxValue Str { get; set; }

            [JsonProperty("dex")]
            public MinMaxValue Dex { get; set; }

            [JsonProperty("int")]
            public MinMaxValue Int { get; set; }
        }

        public partial class SocketFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public SocketFilterDef Value { get; set; }
        }

        public partial class SocketFilterDef : BaseFilterDef
        {
            [JsonProperty("sockets")]
            public SocketsDef Sockets { get; set; }

            [JsonProperty("links")]
            public SocketsDef Links { get; set; }
        }

        public partial class SocketsDef : BaseFilterDef
        {
            [JsonProperty("r")]
            public long? R { get; set; }

            [JsonProperty("b")]
            public long? B { get; set; }

            [JsonProperty("w")]
            public long? W { get; set; }

            [JsonProperty("g")]
            public long? G { get; set; }

            [JsonProperty("min")]
            public long? Min { get; set; }

            [JsonProperty("max")]
            public long? Max { get; set; }
        }

        public partial class TradeFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public TradeFilterDef Value { get; set; }
        }

        public partial class TradeFilterDef : BaseFilterDef
        {
            [JsonProperty("price")]
            public Price Price { get; set; }

            [JsonProperty("account")]
            public Account Account { get; set; }
        }

        public partial class Account : BaseFilterDef
        {
            [JsonProperty("input")]
            public string Input { get; set; }
        }

        public partial class Price : BaseFilterDef
        {
            [JsonProperty("option")]
            public string Option { get; set; }

            [JsonProperty("min")]
            public long? Min { get; set; }

            [JsonProperty("max")]
            public long? Max { get; set; }
        }

        public partial class TypeFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public TypeFilterDef Value { get; set; }
        }

        public partial class TypeFilterDef : BaseFilterDef
        {
            [JsonProperty("category")]
            public OptionValue Category { get; set; }

            [JsonProperty("rarity")]
            public OptionValue Rarity { get; set; }
        }

        public partial class WeaponFilters : BaseFilter
        {
            [JsonProperty("filters")]
            public WeaponFilterDef Value { get; set; }
        }

        public partial class WeaponFilterDef : BaseFilterDef
        {
            [JsonProperty("damage")]
            public MinMaxValue Damage { get; set; }

            [JsonProperty("crit")]
            public MinMaxValue Crit { get; set; }

            [JsonProperty("pdps")]
            public MinMaxValue Pdps { get; set; }

            [JsonProperty("aps")]
            public MinMaxValue Aps { get; set; }

            [JsonProperty("dps")]
            public MinMaxValue Dps { get; set; }

            [JsonProperty("edps")]
            public MinMaxValue Edps { get; set; }
        }

        public partial class FilterGroup : BaseFilter
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("filters")]
            public Filter[] Filters { get; set; }

            [JsonProperty("value")]
            public MinMaxValue GroupValue { get; set; }
        }
        
        public partial class Filter
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("value")]
            public MinMaxValue Value { get; set; }

            [JsonProperty("disabled")]
            public bool Disabled { get; set; }
        }

        public partial class Sort
        {
            [JsonProperty("price")]
            public string Price { get; set; }
        }
    }
}