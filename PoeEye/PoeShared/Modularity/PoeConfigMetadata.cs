using System;
using JetBrains.Annotations;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PoeShared.Modularity;

public record PoeConfigMetadata : IPoeEyeConfig
{
    public string AssemblyName { get; set; }
        
    public string TypeName { get; set; }
        
    public int? Version { get; set; }
        
    [ComparisonIgnore]
    public JToken ConfigValue { get; set; }

    public PoeConfigMetadata()
    {
    }

    public PoeConfigMetadata(Type type) 
    {
        if (type == null)
        {
            return;
        }
        AssemblyName = type.Assembly.GetName().Name;
        TypeName = type.FullName;
    }

    public override string ToString()
    {
        return $"Metadata for {TypeName} (v{Version}) in {AssemblyName}";
    }
}

public sealed record PoeConfigMetadata<T> : PoeConfigMetadata where T : IPoeEyeConfig
{
    public PoeConfigMetadata()
    {
    }

    public PoeConfigMetadata(T value) : base(value?.GetType())
    {
        if (value == null)
        {
            return;
        }
        Value = value;
        if (value is IPoeEyeConfigVersioned configVersioned)
        {
            Version = configVersioned.Version;
        }
    }

    [JsonIgnore]
    public T Value { get; set; }

    public override string ToString()
    {
        return base.ToString();
    }
}