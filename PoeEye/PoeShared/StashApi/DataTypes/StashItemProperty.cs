using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItemProperty
    {
        [DeserializeAs(Name = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "values")]
        [JsonProperty("values")]
        public List<object> Values { get; set; }

        [DeserializeAs(Name = "displayMode")]
        [JsonProperty("displayMode")]
        public int DisplayMode { get; set; }
    }
}
