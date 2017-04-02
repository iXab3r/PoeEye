using System.Collections.Generic;
using Newtonsoft.Json;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.StashRealtimeApi.API
{
    public struct StashTab
    {
        [JsonProperty("accountName")]
        public string AccountName { get; set; }

        [JsonProperty("lastCharacterName")]
        public string LastCharacterName { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("stash")]
        public string Stash { get; set; }

        [JsonProperty("stashType")]
        public string StashType { get; set; }

        [JsonProperty("public")]
        public bool Public { get; set; }

        [JsonProperty("items")]
        public List<StashItem> Items { get; set; }
    }
}