using System;
using System.Linq;
using System.Security;
using Newtonsoft.Json;
using PoeShared.Converters;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

public struct UpdateSourceInfo
{
    public string Id { get; set; }
    
    public string[] Uris { get; set; }
    
    public string Name { get; set; }

    public bool RequiresAuthentication { get; set; }

    [JsonConverter(typeof(SafeDataConverter))] public SecureString Username { get; set; }

    [JsonConverter(typeof(SafeDataConverter))] public SecureString Password { get; set; }

    [JsonIgnore] public bool IsValid => 
        !string.IsNullOrEmpty(Id) && 
        !string.IsNullOrEmpty(Name) && 
        Uris?.Length > 0 && Uris.All(x => !string.IsNullOrEmpty(x));

    public bool Equals(UpdateSourceInfo other)
    {
        var urisAreEqual = Uris == other.Uris || Uris != null && other.Uris != null && Uris.SequenceEqual(other.Uris);
        return urisAreEqual && Name == other.Name && RequiresAuthentication == other.RequiresAuthentication;
    }

    public override bool Equals(object obj)
    {
        return obj is UpdateSourceInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uris, Name, RequiresAuthentication);
    }

    public static bool operator ==(UpdateSourceInfo left, UpdateSourceInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UpdateSourceInfo left, UpdateSourceInfo right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{nameof(Uris)}: {Uris.DumpToString()}, {nameof(Name)}: {Name}";
    }
}