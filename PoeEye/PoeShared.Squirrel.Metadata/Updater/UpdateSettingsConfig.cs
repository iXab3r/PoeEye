using System;
using PoeShared.Modularity;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Persists updater preferences for the current profile; its version is a migration/reset boundary.
/// </summary>
public sealed record UpdateSettingsConfig : IPoeEyeConfigVersioned
{
    public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);

    public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;

    public string UpdateSourceId { get; set; }

    public string PreferredChangelogLanguageId { get; set; } = "en";

    public bool IgnoreDeltaUpdates { get; set; } = true;
    
    public bool AutomaticallyDownloadUpdates { get; set; } = false;

    /// <summary>
    /// Defers automatic source selection until the pack author supplies the updater configuration.
    /// The default preserves ordinary native startup for existing profiles.
    /// </summary>
    public bool AwaitAuthorUpdateConfiguration { get; set; }

    public int Version { get; set; } = 11;
}
