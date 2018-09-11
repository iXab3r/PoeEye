using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItemExtendedInfo
    {
        [JsonProperty("pdps")]
        [DeserializeAs(Name = "pdps")]
        public double? Pdps { get; set; }

        [JsonProperty("aps")]
        [DeserializeAs(Name = "aps")]
        public double? Aps { get; set; }

        [JsonProperty("dps")]
        [DeserializeAs(Name = "dps")]
        public double? Dps { get; set; }

        [JsonProperty("edps")]
        [DeserializeAs(Name = "edps")]
        public double? Edps { get; set; }
    }
}