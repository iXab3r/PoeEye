using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PoeEye.ExileToolsApi.RealtimeApi.Entities
{
    internal sealed class RealtimeQueries : List<RealtimeQuery>
    {
    }

    internal sealed class RealtimeQuery
    {
        public static RealtimeQuery Empty = new RealtimeQuery()
        {
            EqualTo = new Dictionary<string, object>(),
            LessThan = new Dictionary<string, object>(),
            GreaterThan = new Dictionary<string, object>()
        };

        [JsonProperty("eq", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> EqualTo { get; set; }

        [JsonProperty("gt", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> GreaterThan { get; set; }

        [JsonProperty("lt", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> LessThan { get; set; }

        public bool ShouldSerializeEqualTo()
        {
            return EqualTo != null && EqualTo.Any();
        }

        public bool ShouldSerializeGreaterThan()
        {
            return GreaterThan != null && GreaterThan.Any();
        }

        public bool ShouldSerializeLessThan()
        {
            return LessThan != null && LessThan.Any();
        }
    }
}