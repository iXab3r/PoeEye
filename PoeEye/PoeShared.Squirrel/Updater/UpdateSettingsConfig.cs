using System;
using PoeShared.Modularity;

namespace PoeShared.Squirrel.Updater;

public sealed record UpdateSettingsConfig : IPoeEyeConfigVersioned
{
    public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);

    public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;

    public string UpdateSourceId { get; set; }

    public bool IgnoreDeltaUpdates { get; set; } = true;

    public int Version { get; set; } = 11;
}