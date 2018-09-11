using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public List<StashItemDisplayValue> Values { get; set; }

        [DeserializeAs(Name = "displayMode")]
        [JsonProperty("displayMode")]
        public int DisplayMode { get; set; }
    }

    public sealed class StashItemDisplayValue : List<string>
    {
        public string Min => this.Count == 2 ? this[0] : null;
        
        public string Max => this.Count == 2 ? this[1] : null;
        
        public bool IsValid => this.Any(x => !string.IsNullOrWhiteSpace(x));

        public string ToDisplayValue()
        {
            var result = new StringBuilder();
            if (IsValidNumber(Min))
            {
                result.Append(Min);
            }
            if (IsValidNumber(Max))
            {
                result.Append("-");
                result.Append(Max);
            }
            return result.ToString();
        }

        private bool IsValidNumber(string value)
        {
            return double.TryParse(value, out var result) && result > 0;
        }
    }
}