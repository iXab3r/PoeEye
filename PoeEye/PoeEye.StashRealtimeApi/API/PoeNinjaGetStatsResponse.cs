using Newtonsoft.Json;

namespace PoeEye.StashRealtimeApi.API
{
    public struct PoeNinjaGetStatsResponse
    {
        [JsonProperty("nextChangeId")]
        public string NextChangeId { get; set; }
    }
}