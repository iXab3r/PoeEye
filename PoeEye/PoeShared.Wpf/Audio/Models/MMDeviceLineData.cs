using Newtonsoft.Json;

namespace PoeShared.Audio.Models;

public readonly struct MMDeviceLineData
{
    public static readonly MMDeviceLineData All = new MMDeviceLineData("all", "All microphones");
        
    public MMDeviceLineData(string lineId, string name)
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

    public bool Equals(MMDeviceLineData other)
    {
        return string.Equals(LineId, other.LineId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is MMDeviceLineData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return LineId != null ? LineId.GetHashCode() : 0;
    }
}