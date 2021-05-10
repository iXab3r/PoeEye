using System;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public PoeConfigMetadata()
        {
        }

        public PoeConfigMetadata(T value) : this()
        {
            if (value == null)
            {
                return;
            }
            
            Value = value;
            var valueType = value.GetType();
            AssemblyName = valueType.Assembly.GetName().Name;
            TypeName = valueType.FullName;
            if (value is IPoeEyeConfigVersioned configVersioned)
            {
                Version = configVersioned.Version;
            }
        }

        [JsonIgnore]
        public T Value { get; set; }
    }
}