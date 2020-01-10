using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Services;

namespace PoeShared.Modularity
{
    public class PoeConfigMetadata : IPoeEyeConfig
    {
        public string AssemblyName { get; set; }
        
        public string TypeName { get; set; }
        
        public int? Version { get; set; }
        
        [ComparisonIgnore]
        public JToken ConfigValue { get; set; }

        public override string ToString()
        {
            return $"({nameof(PoeConfigMetadata)} {nameof(AssemblyName)}: {AssemblyName}, {nameof(TypeName)}: {TypeName}, {nameof(Version)}: {Version})";
        }
    }

    public sealed class PoeConfigMetadata<T> : PoeConfigMetadata where T : IPoeEyeConfig
    {
        [JsonIgnore]
        public T Value { get; set; }
    }
}