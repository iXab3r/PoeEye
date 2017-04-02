using Newtonsoft.Json;

namespace PoeEye.StashRealtimeApi.API
{
    public struct PoeRatesLastChangeIdResponse
    {
        [JsonProperty("changeId")]
        public string ChangeId { get; set; }
    }
}