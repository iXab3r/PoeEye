using System;
using System.Security;
using Newtonsoft.Json;
using PoeShared.Converters;

namespace PoeShared.Squirrel.Updater;

public struct UpdateSourceInfo
{
    public string Uri { get; set; }

    public string Description { get; set; }

    public bool RequiresAuthentication { get; set; }

    [JsonConverter(typeof(SafeDataConverter))] public SecureString Username { get; set; }

    [JsonConverter(typeof(SafeDataConverter))] public SecureString Password { get; set; }

    [JsonIgnore] public bool IsValid => !string.IsNullOrEmpty(Uri);

    public bool Equals(UpdateSourceInfo other)
    {
        return Uri == other.Uri && Description == other.Description && RequiresAuthentication == other.RequiresAuthentication;
    }

    public override bool Equals(object obj)
    {
        return obj is UpdateSourceInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uri, Description, RequiresAuthentication);
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
        return $"{nameof(Uri)}: {Uri}, {nameof(Description)}: {Description}";
    }
}