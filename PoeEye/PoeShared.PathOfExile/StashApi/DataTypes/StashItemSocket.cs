using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItemSocket
    {
        [JsonProperty("attr")]
        [DeserializeAs(Name = "attr")]
        public string Attribute { get; set; }

        [JsonProperty("group")]
        [DeserializeAs(Name = "group")]
        public int Group { get; set; }

        [JsonProperty("sColour")]
        [DeserializeAs(Name = "sColour")]
        public string Colour { get; set; }
    }
}