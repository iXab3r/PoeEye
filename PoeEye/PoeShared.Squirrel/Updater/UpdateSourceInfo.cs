using System;
using System.Linq;
using System.Security;
using Newtonsoft.Json;
using PoeShared.Converters;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

public sealed record UpdateSourceInfo
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
        if (other == null)
        {
            return false;
        }
        var urisAreEqual = Uris == other.Uris || Uris != null && other.Uris != null && Uris.SequenceEqual(other.Uris);
        return urisAreEqual && Id == other.Id && Name == other.Name && RequiresAuthentication == other.RequiresAuthentication;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uris, Id, Name, RequiresAuthentication);
    }

    public override string ToString()
    {
        return $"{nameof(Uris)}: {Uris.DumpToString()}, {nameof(Name)}: {Name}";
    }
}