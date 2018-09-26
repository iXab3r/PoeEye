using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    internal class Stash : IStash
    {
        [DeserializeAs(Name = "items")]
        [JsonProperty("items")]
        public List<StashItem> Items { get; set; } = new List<StashItem>();

        [DeserializeAs(Name = "tabs")]
        [JsonProperty("tabs")]
        public List<Tab> Tabs { get; set; } = new List<Tab>();

        [DeserializeAs(Name = "numTabs")]
        [JsonProperty("numTabs")]
        public int NumTabs { get; set; }

        IEnumerable<IStashItem> IStash.Items => Items;

        IEnumerable<IStashTab> IStash.Tabs => Tabs;
    }
}