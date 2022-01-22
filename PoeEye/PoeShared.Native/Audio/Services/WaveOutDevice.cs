using System;
using NAudio.Wave;

namespace PoeShared.Audio.Services;

public sealed class WaveOutDevice
{
    public static readonly WaveOutDevice DefaultDevice = new()
    {
        DeviceNumber = -1,
    };
    private readonly Lazy<string> idProvider;

    public WaveOutDevice()
    {
        idProvider = new Lazy<string>(() => new {Name, WaveOutCapabilities.ProductGuid, WaveOutCapabilities.ManufacturerGuid, WaveOutCapabilities.NameGuid}.ToString());
    }
        
    public int DeviceNumber { get; init; }
            
    public WaveOutCapabilities WaveOutCapabilities { get; init; }

    public string Id => idProvider.Value;

    public string Name => MultimediaDeviceName ?? WaveOutCapabilities.ProductName ?? "Default";
        
    public string MultimediaDeviceName { get; init; }

    public override string ToString()
    {
        return $"WaveOut#{DeviceNumber}, Id: {Id}, {new { WaveOutCapabilities.ProductName, WaveOutCapabilities.ManufacturerGuid, WaveOutCapabilities.NameGuid, WaveOutCapabilities.ProductGuid, WaveOutCapabilities.Channels }}";
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || obj is WaveOutDevice other && Equals(other);
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(Id);
    }

    private bool Equals(WaveOutDevice other)
    {
        return Id == other?.Id;
    }
}