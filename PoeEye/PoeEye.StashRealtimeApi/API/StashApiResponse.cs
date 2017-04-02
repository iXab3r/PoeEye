using System.Collections.Generic;
using Newtonsoft.Json;

namespace PoeEye.StashRealtimeApi.API
{
    public struct StashApiResponse
    {
        [JsonProperty("next_change_id")]
        public string NextChangeId { get; set; }

        [JsonProperty("stashes")]
        public List<StashTab> Stashes { get; set; }
    }
}