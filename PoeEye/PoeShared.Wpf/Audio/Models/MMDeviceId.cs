using Newtonsoft.Json;

namespace PoeShared.Audio.Models;

public readonly struct MMDeviceId
{
    public static readonly MMDeviceId All = new MMDeviceId("all", "All devices");
        
    public MMDeviceId(string lineId, string name)
    {
        LineId = lineId;
        Name = name;
    }

    public string LineId { get; }

    public string Name { get; }

    [JsonIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(LineId);

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(LineId)}: {LineId}";
    }

    public bool Equals(MMDeviceId other)
    {
        return string.Equals(LineId, other.LineId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is MMDeviceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return LineId != null ? LineId.GetHashCode() : 0;
    }
}