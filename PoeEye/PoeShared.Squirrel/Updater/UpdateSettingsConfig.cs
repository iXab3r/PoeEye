using System;
using System.Linq;
using PoeShared.Modularity;

namespace PoeShared.Squirrel.Updater
{
    public sealed record UpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);

        public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;

        public UpdateSourceInfo UpdateSource { get; set; } 
        
        public bool IgnoreDeltaUpdates { get; set; }

        public int Version { get; set; } = 10;
    }
}